using Microsoft.EntityFrameworkCore;
using TaskScheduler.Core.Interfaces.Repositories;
using TaskScheduler.Core.Models;
using TaskScheduler.Infrastructure.Data;

namespace TaskScheduler.Infrastructure.Repositories;

/// <summary>
/// Repository for task execution audit trail records.
/// </summary>
public class TaskExecutionRepository : ITaskExecutionRepository
{
    private readonly TaskSchedulerDbContext _context;

    public TaskExecutionRepository(TaskSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<TaskExecution> CreateAsync(TaskExecution execution, CancellationToken ct = default)
    {
        _context.TaskExecutions.Add(execution);
        await _context.SaveChangesAsync(ct);
        return execution;
    }

    public async Task UpdateAsync(TaskExecution execution, CancellationToken ct = default)
    {
        _context.TaskExecutions.Update(execution);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Gets all execution records for a task, most recent first.
    /// </summary>
    public async Task<List<TaskExecution>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _context.TaskExecutions
            .Where(e => e.TaskId == taskId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }
}
