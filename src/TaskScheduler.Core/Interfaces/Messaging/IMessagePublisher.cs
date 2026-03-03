using TaskScheduler.Core.Models;

namespace TaskScheduler.Core.Interfaces.Messaging;

public interface IMessagePublisher : IDisposable
{
    Task PublishAsync(TaskItem task, CancellationToken ct = default);
    Task PublishToDeadLetterAsync(TaskItem task, string reason, CancellationToken ct = default);
}
