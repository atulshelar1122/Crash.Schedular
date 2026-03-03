using System.Diagnostics;
using System.Text.Json;
using TaskScheduler.Core.Interfaces.Handlers;
using TaskScheduler.Core.Interfaces.Messaging;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Worker.Services;

/// <summary>
/// Executes a single task. Called by WorkerHostedService when a message arrives from RabbitMQ.
///
/// Flow:
/// 1. Deserialize task from JSON
/// 2. Set status = Running, start heartbeat timer
/// 3. Look up handler by TaskType
/// 4. Execute handler
/// 5. On success: mark Completed, record execution
/// 6. On failure: retry with backoff or mark Failed, record execution
/// </summary>
public class TaskExecutor
{
    private readonly ITaskRepository _taskRepo;
    private readonly ITaskExecutionRepository _executionRepo;
    private readonly ITaskHandlerRegistry _handlerRegistry;
    private readonly IMessagePublisher _publisher;
    private readonly WorkerInfo _workerInfo;
    private readonly ILogger<TaskExecutor> _logger;
    private readonly IConfiguration _config;

    public TaskExecutor(
        ITaskRepository taskRepo,
        ITaskExecutionRepository executionRepo,
        ITaskHandlerRegistry handlerRegistry,
        IMessagePublisher publisher,
        WorkerInfo workerInfo,
        ILogger<TaskExecutor> logger,
        IConfiguration config)
    {
        _taskRepo = taskRepo;
        _executionRepo = executionRepo;
        _handlerRegistry = handlerRegistry;
        _publisher = publisher;
        _workerInfo = workerInfo;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Process a raw JSON message from RabbitMQ. Returns true if the message should be ACKed.
    /// </summary>
    public async Task<bool> ExecuteAsync(string messageJson, CancellationToken ct)
    {
        TaskItem? taskMessage;
        try
        {
            taskMessage = JsonSerializer.Deserialize<TaskItem>(messageJson);
            if (taskMessage == null)
            {
                _logger.LogError("Failed to deserialize task message");
                return false;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid task message JSON");
            return false; // NACK - bad message
        }

        // Fetch fresh task state from database
        var task = await _taskRepo.GetByIdAsync(taskMessage.TaskId, ct);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found in database, skipping", taskMessage.TaskId);
            return true; // ACK - task was deleted
        }

        if (task.Status == TaskItemStatus.Cancelled)
        {
            _logger.LogInformation("Task {TaskId} was cancelled, skipping", task.TaskId);
            return true; // ACK - cancelled
        }

        // Mark as Running
        task.Status = TaskItemStatus.Running;
        task.WorkerId = _workerInfo.WorkerId;
        task.LastHeartbeat = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);

        // Start heartbeat timer
        var heartbeatInterval = _config.GetValue<int>("Worker:HeartbeatIntervalSeconds", 30);
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = RunHeartbeatAsync(task.TaskId, heartbeatInterval, heartbeatCts.Token);

        // Create execution record
        var execution = new TaskExecution
        {
            TaskId = task.TaskId,
            WorkerId = _workerInfo.WorkerId,
            StartTime = DateTime.UtcNow,
            Status = "Running",
            RetryCount = task.CurrentRetryCount
        };
        await _executionRepo.CreateAsync(execution, ct);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Find handler
            var handler = _handlerRegistry.GetHandler(task.TaskType);
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler registered for task type '{task.TaskType}'");
            }

            // Execute
            _logger.LogInformation("Executing task {TaskId} ({TaskType}) on worker {WorkerId}",
                task.TaskId, task.TaskType, _workerInfo.WorkerId);

            var result = await handler.ExecuteAsync(task.Payload, ct);
            stopwatch.Stop();

            if (result.IsSuccess)
            {
                await HandleSuccessAsync(task, execution, stopwatch.ElapsedMilliseconds, ct);
            }
            else
            {
                await HandleFailureAsync(task, execution, result.ErrorMessage ?? "Handler returned failure",
                    null, stopwatch.ElapsedMilliseconds, ct);
            }

            return true; // ACK
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await HandleFailureAsync(task, execution, ex.Message, ex.StackTrace,
                stopwatch.ElapsedMilliseconds, ct);
            return true; // ACK — retry is handled via DB, not requeue
        }
        finally
        {
            heartbeatCts.Cancel();
            try { await heartbeatTask; } catch (OperationCanceledException) { }
        }
    }

    private async Task HandleSuccessAsync(TaskItem task, TaskExecution execution, long durationMs, CancellationToken ct)
    {
        task.Status = TaskItemStatus.Completed;
        task.ExecutedAt = DateTime.UtcNow;
        task.ExecutionTimeMs = (int)durationMs;
        await _taskRepo.UpdateAsync(task, ct);

        execution.Status = "Completed";
        execution.EndTime = DateTime.UtcNow;
        execution.ExecutionTimeMs = (int)durationMs;
        await _executionRepo.UpdateAsync(execution, ct);

        _logger.LogInformation("Task {TaskId} completed in {Duration}ms", task.TaskId, durationMs);
    }

    private async Task HandleFailureAsync(TaskItem task, TaskExecution execution,
        string errorMessage, string? stackTrace, long durationMs, CancellationToken ct)
    {
        // Record the execution failure
        execution.Status = "Failed";
        execution.EndTime = DateTime.UtcNow;
        execution.ExecutionTimeMs = (int)durationMs;
        execution.ErrorMessage = errorMessage;
        execution.ErrorStackTrace = stackTrace;
        await _executionRepo.UpdateAsync(execution, ct);

        if (task.CurrentRetryCount < task.MaxRetries)
        {
            // Schedule retry with exponential backoff
            task.CurrentRetryCount++;
            task.Status = TaskItemStatus.Pending;
            task.ScheduledTime = RetryPolicy.CalculateNextRetryTime(task.CurrentRetryCount);
            task.WorkerId = null;
            task.LastHeartbeat = null;
            task.ErrorMessage = errorMessage;

            _logger.LogWarning("Task {TaskId} failed, scheduling retry {Retry}/{MaxRetries} at {NextRetry}",
                task.TaskId, task.CurrentRetryCount, task.MaxRetries, task.ScheduledTime);
        }
        else
        {
            // Max retries exhausted — mark as failed and send to dead letter queue
            task.Status = TaskItemStatus.Failed;
            task.ErrorMessage = errorMessage;
            task.ExecutionTimeMs = (int)durationMs;

            await _publisher.PublishToDeadLetterAsync(task, errorMessage, ct);

            _logger.LogError("Task {TaskId} permanently failed after {MaxRetries} retries: {Error}",
                task.TaskId, task.MaxRetries, errorMessage);
        }

        await _taskRepo.UpdateAsync(task, ct);
    }

    /// <summary>
    /// Periodically updates the task's LastHeartbeat to prove this worker is still alive.
    /// The StaleTaskMonitor will reset tasks whose heartbeat is too old.
    /// </summary>
    private async Task RunHeartbeatAsync(Guid taskId, int intervalSeconds, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);

            try
            {
                var task = await _taskRepo.GetByIdAsync(taskId, ct);
                if (task != null && task.Status == TaskItemStatus.Running)
                {
                    task.LastHeartbeat = DateTime.UtcNow;
                    await _taskRepo.UpdateAsync(task, ct);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Failed to update heartbeat for task {TaskId}", taskId);
            }
        }
    }
}
