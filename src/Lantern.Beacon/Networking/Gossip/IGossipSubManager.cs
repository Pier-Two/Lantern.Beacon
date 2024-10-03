using ITopic = Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.ITopic;

namespace Lantern.Beacon.Networking.Gossip;

public interface IGossipSubManager
{
    ITopic? LightClientFinalityUpdate { get; }
    
    ITopic? LightClientOptimisticUpdate { get; }
    
    void Init();
    
    void Start(CancellationToken token = default);
    
    Task StopAsync();
}