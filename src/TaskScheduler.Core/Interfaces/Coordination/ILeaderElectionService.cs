namespace TaskScheduler.Core.Interfaces.Coordination;

public interface ILeaderElectionService : IDisposable
{
    bool IsLeader { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    event EventHandler<bool>? LeadershipChanged;
}
