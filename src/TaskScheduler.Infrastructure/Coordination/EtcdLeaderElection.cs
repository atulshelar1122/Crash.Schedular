using dotnet_etcd;
using Etcdserverpb;
using Microsoft.Extensions.Logging;

namespace TaskScheduler.Infrastructure.Coordination;

/// <summary>
/// Leader election using etcd's lease mechanism.
/// - Creates a lease with 10-second TTL
/// - Attempts to write a key; first writer becomes leader
/// - Leader renews lease every 5 seconds to stay alive
/// - If leader dies (lease expires), followers detect and compete for leadership
/// </summary>
public class EtcdLeaderElection : Core.Interfaces.Coordination.ILeaderElectionService
{
    private readonly ILogger<EtcdLeaderElection> _logger;
    private readonly EtcdClient _client;
    private readonly string _nodeId;
    private readonly string _electionKey = "/task-scheduler/leader";
    private readonly int _leaseTtlSeconds = 10;

    private long _leaseId;
    private bool _isLeader;
    private CancellationTokenSource? _cts;
    private Task? _keepAliveTask;

    public bool IsLeader => _isLeader;
    public event EventHandler<bool>? LeadershipChanged;

    public EtcdLeaderElection(string etcdEndpoint, string nodeId, ILogger<EtcdLeaderElection> logger)
    {
        _client = new EtcdClient(etcdEndpoint, configureChannelOptions: options =>
        {
            options.Credentials = Grpc.Core.ChannelCredentials.Insecure;
        });
        _nodeId = nodeId;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _logger.LogInformation("Node {NodeId} starting leader election", _nodeId);

        await CampaignForLeadershipAsync();

        // Background task: renew lease if leader, or watch for leader changes if follower
        _keepAliveTask = Task.Run(() => RunLeaderLoopAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Attempts to become leader by creating a lease and writing the election key.
    /// Uses a transaction: only succeeds if the key doesn't already exist (CreateRevision == 0).
    /// </summary>
    private async Task CampaignForLeadershipAsync()
    {
        try
        {
            // Create a new lease
            var leaseResponse = await _client.LeaseGrantAsync(new LeaseGrantRequest
            {
                TTL = _leaseTtlSeconds
            });
            _leaseId = leaseResponse.ID;

            // Try to create the key with our lease (only if it doesn't exist)
            var txnRequest = new TxnRequest();
            txnRequest.Compare.Add(new Compare
            {
                Key = Google.Protobuf.ByteString.CopyFromUtf8(_electionKey),
                Result = Compare.Types.CompareResult.Equal,
                Target = Compare.Types.CompareTarget.Create,
                CreateRevision = 0 // Key does not exist
            });
            txnRequest.Success.Add(new RequestOp
            {
                RequestPut = new PutRequest
                {
                    Key = Google.Protobuf.ByteString.CopyFromUtf8(_electionKey),
                    Value = Google.Protobuf.ByteString.CopyFromUtf8(_nodeId),
                    Lease = _leaseId
                }
            });

            var txnResponse = await _client.TransactionAsync(txnRequest);

            if (txnResponse.Succeeded)
            {
                SetLeadership(true);
                _logger.LogInformation("Node {NodeId} elected as leader", _nodeId);
            }
            else
            {
                SetLeadership(false);
                _logger.LogInformation("Node {NodeId} is a follower", _nodeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during leader election campaign");
            SetLeadership(false);
        }
    }

    /// <summary>
    /// Main loop: if leader, renew lease every 5s. If follower, periodically try to become leader.
    /// </summary>
    private async Task RunLeaderLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_isLeader)
                {
                    // Renew lease to stay as leader
                    await _client.LeaseKeepAlive(_leaseId, ct);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
                else
                {
                    // Wait and try again to become leader
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    await CampaignForLeadershipAsync();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in leader loop for node {NodeId}", _nodeId);
                SetLeadership(false);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }

    private void SetLeadership(bool isLeader)
    {
        if (_isLeader != isLeader)
        {
            _isLeader = isLeader;
            LeadershipChanged?.Invoke(this, isLeader);
            _logger.LogInformation("Node {NodeId} leadership changed to {IsLeader}", _nodeId, isLeader);
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Node {NodeId} stopping leader election", _nodeId);
        _cts?.Cancel();

        if (_keepAliveTask != null)
        {
            try { await _keepAliveTask; } catch (OperationCanceledException) { }
        }

        // Revoke lease to immediately release leadership
        if (_leaseId > 0)
        {
            try
            {
                await _client.LeaseRevokeAsync(new LeaseRevokeRequest { ID = _leaseId });
                _logger.LogInformation("Node {NodeId} revoked lease", _nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error revoking lease for node {NodeId}", _nodeId);
            }
        }

        SetLeadership(false);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _client?.Dispose();
    }
}
