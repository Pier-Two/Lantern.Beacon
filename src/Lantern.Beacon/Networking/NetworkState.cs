using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public class NetworkState : INetworkState
{
     private readonly object _peerCountLock = new();

     public ConcurrentDictionary<PeerId, RepeatedField<string>> PeerProtocols { get; } = new();

     public IEnumerable<IProtocol> AppLayerProtocols { get; private set; } 

     public MetaData MetaData { get; private set; } = MetaData.CreateDefault();

     public int PeerCount { get; private set; }
     
     public void Init(IEnumerable<IProtocol> appLayerProtocols)
     {
          AppLayerProtocols = appLayerProtocols;
          MetaData = MetaData.CreateDefault();
     }

     public void IncrementPeerCount()
     {
          lock (_peerCountLock) 
          {
               PeerCount++;
          }
     }
     
     public void DecrementPeerCount()
     {
          lock (_peerCountLock)
          {
               if (PeerCount > 0)
               {
                    PeerCount--;
               }
               else
               {
                    throw new InvalidOperationException("PeerCount cannot be negative.");
               }
          }
     }
}