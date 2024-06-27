using Multiformats.Address;
using Nethermind.Libp2p.Core.Discovery;

namespace Lantern.Beacon.Networking.Discovery;

public class ManualDiscoveryProtocol : IDiscoveryProtocol
{
    public Task DiscoverAsync(Multiaddress localPeerAddr, CancellationToken token = default) => Task.CompletedTask;

    public Func<Multiaddress[], bool>? OnAddPeer { get; set; } 
    
    public Func<Multiaddress[], bool>? OnRemovePeer { get; set; }
}