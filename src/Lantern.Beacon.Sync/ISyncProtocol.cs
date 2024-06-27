using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Deneb;
using Lantern.Beacon.Sync.Types.Phase0;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Sync;

public interface ISyncProtocol
{
    ILogger<SyncProtocol>? Logger { get; }
    
    MetaData MetaData { get; }
    
    AltairLightClientStore AltairLightClientStore { get; }
    
    CapellaLightClientStore CapellaLightClientStore { get; }
    
    DenebLightClientStore DenebLightClientStore { get; }

    SyncProtocolOptions Options { get; }
    
    void Init();

    void InitialiseStoreFromAltairBootstrap(byte[] trustedBlockRoot, AltairLightClientBootstrap bootstrap);

    void InitialiseStoreFromCapellaBootstrap(byte[] trustedBlockRoot, CapellaLightClientBootstrap bootstrap);

    void InitialiseStoreFromDenebBootstrap(byte[] trustedBlockRoot, DenebLightClientBootstrap bootstrap);
}