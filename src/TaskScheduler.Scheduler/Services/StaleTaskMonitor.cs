using TaskScheduler.Core.Interfaces.Coordination;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Scheduler.Services;

/// <summary>
/// Every 1 minute (if leader): finds tasks stuck in "Running" status where the worker
/// hasn't sent a heartbeat within the timeout (2 min). These are from crashed workers.
/// Resets them to Pending for retry, or marks as Failed if retries exhausted.
/// </summary>
public class StaleTaskMonitor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILeaderElectionService _leaderElection;
    private readonly ILogger<StaleTaskMonitor> _logger;
    private readonly IConfiguration _config;

    public StaleTaskMonitor(
        IServiceProvider serviceProvider,
        ILeaderElectionService leaderElection,
        ILogger<StaleTaskMonitor> logger,
        IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _leaderElection = leaderElection;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stale task monitor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_leaderElection.IsLeader)
                {
                    await DetectAndRecoverStaleTasks(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stale task monitor");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task DetectAndRecoverStaleTasks(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var timeoutMinutes = _config.GetValue<int>("Scheduler:HeartbeatTimeoutMinutes", 2);
        var staleTasks = await taskRepo.GetStaleTasksAsync(TimeSpan.FromMinutes(timeoutMinutes), ct);

        if (staleTasks.Count == 0)
            return;

        _logger.LogWarning("Found {Count} stale tasks (heartbeat timeout > {Timeout} min)",
            staleTasks.Count, timeoutMinutes);

        foreach (var task in staleTasks)
        {
            if (task.CurrentRetryCount < task.MaxRetries)
            {
                // Reset to Pending with backoff delay for retry
                task.Status = TaskItemStatus.Pending;
                task.ScheduledTime = RetryPolicy.CalculateNextRetryTime(task.CurrentRetryCount);
                task.CurrentRetryCount++;
                task.WorkerId = null;
                task.LastHeartbeat = null;

                _logger.LogWarning(
                    "Stale task {TaskId} reset to Pending for retry {Retry}/{MaxRetries}. Next attempt: {NextAttempt}",
                    task.TaskId, task.CurrentRetryCount, task.MaxRetries, task.ScheduledTime);
            }
            else
            {
                // Exhausted retries — mark as Failed
                task.Status = TaskItemStatus.Failed;
                task.ErrorMessage = "Task timed out: worker heartbeat expired after max retries";

                _logger.LogError("Stale task {TaskId} marked as Failed after {MaxRetries} retries",
                    task.TaskId, task.MaxRetries);
            }

            await taskRepo.UpdateAsync(task, ct);
        }
    }
}
