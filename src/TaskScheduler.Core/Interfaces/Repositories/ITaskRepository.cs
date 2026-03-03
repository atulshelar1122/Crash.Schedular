using TaskScheduler.Core.DTOs;
using TaskScheduler.Core.Models;

namespace TaskScheduler.Core.Interfaces.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default);
    Task<List<TaskItem>> GetPendingTasksAsync(int batchSize, CancellationToken ct = default);
    Task<List<TaskItem>> GetStaleTasksAsync(TimeSpan heartbeatTimeout, CancellationToken ct = default);
    Task<(List<TaskItem> Items, int TotalCount)> GetFilteredAsync(TaskFilter filter, CancellationToken ct = default);
    Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateBatchStatusAsync(List<Guid> taskIds, TaskItemStatus status, CancellationToken ct = default);
    Task<Dictionary<TaskItemStatus, int>> GetTaskCountsByStatusAsync(CancellationToken ct = default);
}
