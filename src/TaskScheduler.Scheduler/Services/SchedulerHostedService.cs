using TaskScheduler.Core.Interfaces.Coordination;
using TaskScheduler.Core.Interfaces.Messaging;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Scheduler.Services;

/// <summary>
/// Main scheduler loop. Every 5 seconds (if this instance is the leader):
/// 1. Queries database for pending tasks that are due
/// 2. Updates their status to Queued
/// 3. Publishes them to RabbitMQ for workers to pick up
/// </summary>
public class SchedulerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILeaderElectionService _leaderElection;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<SchedulerHostedService> _logger;
    private readonly IConfiguration _config;

    public SchedulerHostedService(
        IServiceProvider serviceProvider,
        ILeaderElectionService leaderElection,
        IMessagePublisher publisher,
        ILogger<SchedulerHostedService> logger,
        IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _leaderElection = leaderElection;
        _publisher = publisher;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start leader election
        await _leaderElection.StartAsync(stoppingToken);

        var pollInterval = _config.GetValue<int>("Scheduler:PollIntervalSeconds", 5);
        var batchSize = _config.GetValue<int>("Scheduler:BatchSize", 100);

        _logger.LogInformation("Scheduler started. PollInterval={PollInterval}s, BatchSize={BatchSize}",
            pollInterval, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_leaderElection.IsLeader)
                {
                    await PollAndPublishAsync(batchSize, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Not leader, skipping poll cycle");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduler poll cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollInterval), stoppingToken);
        }

        await _leaderElection.StopAsync(stoppingToken);
    }

    /// <summary>
    /// Fetches pending due tasks, marks them as Queued, publishes to RabbitMQ.
    /// </summary>
    private async Task PollAndPublishAsync(int batchSize, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var pendingTasks = await taskRepo.GetPendingTasksAsync(batchSize, ct);

        if (pendingTasks.Count == 0)
            return;

        _logger.LogInformation("Found {Count} pending tasks to schedule", pendingTasks.Count);

        var queuedIds = new List<Guid>();

        foreach (var task in pendingTasks)
        {
            try
            {
                await _publisher.PublishAsync(task, ct);
                queuedIds.Add(task.TaskId);
                _logger.LogInformation("Task {TaskId} ({TaskType}) published to queue",
                    task.TaskId, task.TaskType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish task {TaskId} to queue", task.TaskId);
            }
        }

        if (queuedIds.Count > 0)
        {
            await taskRepo.UpdateBatchStatusAsync(queuedIds, TaskItemStatus.Queued, ct);
            _logger.LogInformation("Marked {Count} tasks as Queued", queuedIds.Count);
        }
    }
}
