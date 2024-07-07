using System.Collections.Concurrent;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Basic;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Sync;

public interface ISyncProtocol
{
    ILogger<SyncProtocol>? Logger { get; }
    
    AltairLightClientStore AltairLightClientStore { get; }
    
    CapellaLightClientStore CapellaLightClientStore { get; }
    
    DenebLightClientStore DenebLightClientStore { get; }
    
    DenebLightClientOptimisticUpdate PreviousLightClientOptimisticUpdate { get; set; }
    
    DenebLightClientFinalityUpdate PreviousLightClientFinalityUpdate { get; set; }
    
    LightClientUpdatesByRangeRequest? LightClientUpdatesByRangeRequest { get; set; }
    
    SyncProtocolOptions Options { get; }
    
    ForkType ActiveFork { get; }
    
    bool IsInitialised { get; }
    
    void Init();

    bool InitialiseStoreFromAltairBootstrap(byte[] trustedBlockRoot, AltairLightClientBootstrap bootstrap);

    bool InitialiseStoreFromCapellaBootstrap(byte[] trustedBlockRoot, CapellaLightClientBootstrap bootstrap);

    bool InitialiseStoreFromDenebBootstrap(byte[] trustedBlockRoot, DenebLightClientBootstrap bootstrap);
    
    void SetActiveFork(ForkType forkType);
}