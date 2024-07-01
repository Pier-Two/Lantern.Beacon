using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Deneb;
using Lantern.Beacon.Sync.Types.Phase0;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Sync;

public interface ISyncProtocol
{
    ILogger<SyncProtocol>? Logger { get; }
    
    MetaData MetaData { get; }
    
    AltairLightClientStore AltairLightClientStore { get; }
    
    CapellaLightClientStore CapellaLightClientStore { get; }
    
    DenebLightClientStore DenebLightClientStore { get; }
    
    DenebLightClientOptimisticUpdate DenebLightClientOptimisticUpdate { get; }
    
    DenebLightClientFinalityUpdate DenebLightClientFinalityUpdate { get; }

    SyncProtocolOptions Options { get; }
    
    LightClientUpdatesByRangeRequest? LightClientUpdatesByRangeRequest { get; }
    
    ConcurrentDictionary<PeerId, RepeatedField<string>> PeerProtocols { get; } 
    
    int PeerCount { get; set; }
    
    IEnumerable<IProtocol> AppLayerProtocols { get; set; }
    
    ForkType ActiveFork { get; }
    
    bool IsInitialized { get; }
    
    void Init();

    void InitialiseStoreFromAltairBootstrap(byte[] trustedBlockRoot, AltairLightClientBootstrap bootstrap);

    void InitialiseStoreFromCapellaBootstrap(byte[] trustedBlockRoot, CapellaLightClientBootstrap bootstrap);

    void InitialiseStoreFromDenebBootstrap(byte[] trustedBlockRoot, DenebLightClientBootstrap bootstrap);
    
    void SetActiveFork(ForkType forkType);

    void SetLightClientUpdatesByRangeRequest(ulong startPeriod, ulong count);
    
    void SetDenebLightClientOptimisticUpdate(DenebLightClientOptimisticUpdate update);
    
    void SetDenebLightClientFinalityUpdate(DenebLightClientFinalityUpdate update);
}