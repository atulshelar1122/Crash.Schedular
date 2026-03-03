using TaskScheduler.Core.Models;

namespace TaskScheduler.Core.Interfaces.Repositories;

public interface IRecurringTaskRepository
{
    Task<RecurringTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<RecurringTask>> GetDueTasksAsync(CancellationToken ct = default);
    Task<List<RecurringTask>> GetAllAsync(CancellationToken ct = default);
    Task<RecurringTask> CreateAsync(RecurringTask task, CancellationToken ct = default);
    Task UpdateAsync(RecurringTask task, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
