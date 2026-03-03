using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;
using TaskScheduler.Infrastructure.Data;

namespace TaskScheduler.Infrastructure.Repositories;

/// <summary>
/// Repository for managing recurring task templates in PostgreSQL.
/// </summary>
public class RecurringTaskRepository : IRecurringTaskRepository
{
    private readonly TaskSchedulerDbContext _context;
    private readonly ILogger<RecurringTaskRepository> _logger;

    public RecurringTaskRepository(TaskSchedulerDbContext context, ILogger<RecurringTaskRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RecurringTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.RecurringTasks.FindAsync(new object[] { id }, ct);
    }

    /// <summary>
    /// Gets recurring tasks whose NextExecutionTime has passed and are enabled.
    /// The scheduler uses this to spawn new task instances.
    /// </summary>
    public async Task<List<RecurringTask>> GetDueTasksAsync(CancellationToken ct = default)
    {
        return await _context.RecurringTasks
            .Where(t => t.IsEnabled && t.NextExecutionTime <= DateTime.UtcNow)
            .ToListAsync(ct);
    }

    public async Task<List<RecurringTask>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.RecurringTasks
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<RecurringTask> CreateAsync(RecurringTask task, CancellationToken ct = default)
    {
        _context.RecurringTasks.Add(task);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Recurring task {RecurringTaskId} '{Name}' created with cron '{Cron}'",
            task.RecurringTaskId, task.Name, task.CronExpression);
        return task;
    }

    public async Task UpdateAsync(RecurringTask task, CancellationToken ct = default)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.RecurringTasks.Update(task);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _context.RecurringTasks.FindAsync(new object[] { id }, ct);
        if (task != null)
        {
            _context.RecurringTasks.Remove(task);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Recurring task {RecurringTaskId} deleted", id);
        }
    }
}
