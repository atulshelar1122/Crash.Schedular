using TaskScheduler.Core.Models;

namespace TaskScheduler.Core.Interfaces.Repositories;

public interface ITaskExecutionRepository
{
    Task<TaskExecution> CreateAsync(TaskExecution execution, CancellationToken ct = default);
    Task UpdateAsync(TaskExecution execution, CancellationToken ct = default);
    Task<List<TaskExecution>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
}
