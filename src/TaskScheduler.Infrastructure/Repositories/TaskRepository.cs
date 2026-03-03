using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskScheduler.Core.DTOs;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;
using TaskScheduler.Infrastructure.Data;

namespace TaskScheduler.Infrastructure.Repositories;

/// <summary>
/// Repository for managing task entities in PostgreSQL via EF Core.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly TaskSchedulerDbContext _context;
    private readonly ILogger<TaskRepository> _logger;

    public TaskRepository(TaskSchedulerDbContext context, ILogger<TaskRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _context.Tasks.FindAsync(new object[] { taskId }, ct);
    }

    /// <summary>
    /// Gets pending tasks that are due for execution, ordered by priority (highest first) then scheduled time.
    /// </summary>
    public async Task<List<TaskItem>> GetPendingTasksAsync(int batchSize, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Where(t => t.Status == TaskItemStatus.Pending && t.ScheduledTime <= DateTime.UtcNow)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.ScheduledTime)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Finds tasks that are marked as Running but haven't sent a heartbeat within the timeout period.
    /// These are likely from crashed workers.
    /// </summary>
    public async Task<List<TaskItem>> GetStaleTasksAsync(TimeSpan heartbeatTimeout, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - heartbeatTimeout;
        return await _context.Tasks
            .Where(t => t.Status == TaskItemStatus.Running && t.LastHeartbeat < cutoff)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets a filtered, paginated list of tasks for the API.
    /// </summary>
    public async Task<(List<TaskItem> Items, int TotalCount)> GetFilteredAsync(TaskFilter filter, CancellationToken ct = default)
    {
        var query = _context.Tasks.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (!string.IsNullOrEmpty(filter.TaskType))
            query = query.Where(t => t.TaskType == filter.TaskType);

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Task {TaskId} of type {TaskType} created", task.TaskId, task.TaskType);
        return task;
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Batch-updates status for multiple tasks in a single round-trip to the database.
    /// Used by the scheduler to mark tasks as Queued after publishing to RabbitMQ.
    /// </summary>
    public async Task UpdateBatchStatusAsync(List<Guid> taskIds, TaskItemStatus status, CancellationToken ct = default)
    {
        await _context.Tasks
            .Where(t => taskIds.Contains(t.TaskId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);
    }

    /// <summary>
    /// Returns a count of tasks grouped by status. Used for the metrics endpoint.
    /// </summary>
    public async Task<Dictionary<TaskItemStatus, int>> GetTaskCountsByStatusAsync(CancellationToken ct = default)
    {
        return await _context.Tasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }
}
