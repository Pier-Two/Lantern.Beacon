using Nethermind.Libp2p.Protocols.Pubsub;

namespace Lantern.Beacon.Networking.Gossip;

public interface IGossipSubManager
{
    ITopic? LightClientFinalityUpdate { get; }
    
    ITopic? LightClientOptimisticUpdate { get; }
    
    void Init();
    
    Task StartAsync(CancellationToken token = default);
    
    Task StopAsync();
}