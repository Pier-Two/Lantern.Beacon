using Lantern.Discv5.Enr;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public interface ICustomDiscoveryProtocol 
{
    Task<bool> InitAsync();
    
    Task GetDiscoveredNodesAsync(Multiaddress localPeerAddr, CancellationToken token = default);
    
    Task StopAsync();
    
    Func<Multiaddress[], bool>? OnAddPeer { set; get; }
    
    Func<Multiaddress[], bool>? OnRemovePeer { set; get; }
}