namespace TaskScheduler.Core.Interfaces.Messaging;

public interface IMessageConsumer : IDisposable
{
    Task StartConsumingAsync(Func<string, Task<bool>> messageHandler, CancellationToken ct = default);
    Task StopConsumingAsync(CancellationToken ct = default);
}
