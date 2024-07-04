using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public interface INetworkState
{
    ConcurrentDictionary<PeerId, RepeatedField<string>> PeerProtocols { get; }
    
    IEnumerable<IProtocol> AppLayerProtocols { get; }
    
    MetaData MetaData { get; }
    
    int PeerCount { get; }
    
    void Init(IEnumerable<IProtocol> appLayerProtocols);

    void IncrementPeerCount();

    void DecrementPeerCount();
}