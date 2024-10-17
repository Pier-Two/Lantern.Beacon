using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public interface IPeerState
{
    ConcurrentDictionary<PeerId, RepeatedField<string>> PeerProtocols { get; }
    
    ConcurrentDictionary<PeerId, IRemotePeer> BootstrapPeers { get; }
    
    ConcurrentDictionary<PeerId, bool> GossipPeers { get; } 
    
    IEnumerable<IProtocol> AppLayerProtocols { get; }
    
    MetaData MetaData { get; }
    
    void Init(IEnumerable<IProtocol> appLayerProtocols);
}