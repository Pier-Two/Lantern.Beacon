using System.Text;
using IronSnappy;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClient(ISyncProtocol syncProtocol, ILiteDbService liteDbService, IPeerFactoryBuilder peerFactoryBuilder, IPeerState peerState, IBeaconClientManager beaconClientManager, IGossipSubManager gossipSubManager, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public async Task InitAsync(CancellationToken token = default)
    {
        try
        {
            liteDbService.Init();
            
            var altairStore = liteDbService.Fetch<AltairLightClientStore>(nameof(AltairLightClientStore));
            var capellaStore = liteDbService.Fetch<CapellaLightClientStore>(nameof(CapellaLightClientStore));
            var denebStore = liteDbService.Fetch<DenebLightClientStore>(nameof(DenebLightClientStore));
            var finalityUpdate = liteDbService.Fetch<DenebLightClientFinalityUpdate>(nameof(DenebLightClientFinalityUpdate));
            var optimisticUpdate = liteDbService.Fetch<DenebLightClientOptimisticUpdate>(nameof(DenebLightClientOptimisticUpdate));
        
            syncProtocol.Init(altairStore, capellaStore, denebStore, finalityUpdate, optimisticUpdate);
            peerState.Init(peerFactoryBuilder.AppLayerProtocols);
            gossipSubManager.Init();

            if (gossipSubManager.LightClientFinalityUpdate == null || gossipSubManager.LightClientOptimisticUpdate == null)
            {
                return;
            }
            
            await beaconClientManager.InitAsync(token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start peer manager");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        try
        { 
            await gossipSubManager.StartAsync(token);
            await beaconClientManager.StartAsync(token); 
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Beacon client");
            throw;
        }
    }
    
    public async Task StopAsync()
    {
        await gossipSubManager.StopAsync();
        await beaconClientManager.StopAsync();
        liteDbService.Dispose();
    }
}