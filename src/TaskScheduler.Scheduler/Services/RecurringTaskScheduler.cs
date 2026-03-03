using Cronos;
using TaskScheduler.Core.Interfaces.Coordination;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Scheduler.Services;

/// <summary>
/// Every 1 minute (if leader): checks for recurring tasks whose NextExecutionTime has passed,
/// creates a new TaskItem from the template, and advances NextExecutionTime using the cron expression.
/// </summary>
public class RecurringTaskSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILeaderElectionService _leaderElection;
    private readonly ILogger<RecurringTaskSchedulerService> _logger;

    public RecurringTaskSchedulerService(
        IServiceProvider serviceProvider,
        ILeaderElectionService leaderElection,
        ILogger<RecurringTaskSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _leaderElection = leaderElection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recurring task scheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_leaderElection.IsLeader)
                {
                    await ProcessRecurringTasksAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring tasks");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessRecurringTasksAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var recurringRepo = scope.ServiceProvider.GetRequiredService<IRecurringTaskRepository>();
        var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var dueTasks = await recurringRepo.GetDueTasksAsync(ct);

        if (dueTasks.Count == 0)
            return;

        _logger.LogInformation("Found {Count} due recurring tasks", dueTasks.Count);

        foreach (var recurring in dueTasks)
        {
            try
            {
                // Create a new task instance from the recurring template
                var newTask = new TaskItem
                {
                    Name = $"{recurring.Name} ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
                    TaskType = recurring.TaskType,
                    Payload = recurring.Payload ?? "{}",
                    ScheduledTime = DateTime.UtcNow,
                    Priority = recurring.Priority,
                    MaxRetries = recurring.MaxRetries
                };

                await taskRepo.CreateAsync(newTask, ct);

                // Advance NextExecutionTime using cron expression
                var cron = CronExpression.Parse(recurring.CronExpression);
                var nextExecution = cron.GetNextOccurrence(DateTime.UtcNow);

                recurring.LastExecutionTime = DateTime.UtcNow;
                recurring.NextExecutionTime = nextExecution ?? DateTime.UtcNow.AddDays(1);

                await recurringRepo.UpdateAsync(recurring, ct);

                _logger.LogInformation(
                    "Created task {TaskId} from recurring {RecurringId}. Next execution: {NextExecution}",
                    newTask.TaskId, recurring.RecurringTaskId, recurring.NextExecutionTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring task {RecurringId}",
                    recurring.RecurringTaskId);
            }
        }
    }
}
