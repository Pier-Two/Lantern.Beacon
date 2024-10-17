using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public class PeerState : IPeerState
{
     public ConcurrentDictionary<PeerId, RepeatedField<string>> PeerProtocols { get; } = new();
     
     public ConcurrentDictionary<PeerId, IRemotePeer> BootstrapPeers { get; } = new();
     
     public ConcurrentDictionary<PeerId, bool> GossipPeers { get; } = new();

     public IEnumerable<IProtocol> AppLayerProtocols { get; private set; } 

     public MetaData MetaData { get; private set; } = MetaData.CreateDefault();
     
     public void Init(IEnumerable<IProtocol> appLayerProtocols)
     {
          AppLayerProtocols = appLayerProtocols;
          MetaData = MetaData.CreateDefault();
     }
}