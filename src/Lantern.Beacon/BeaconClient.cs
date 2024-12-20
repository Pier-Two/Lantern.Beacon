using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Networking.RestApi;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClient(BeaconClientOptions options, ISyncProtocol syncProtocol, ILiteDbService liteDbService, IPeerFactoryBuilder peerFactoryBuilder, IPeerState peerState, IHttpServer httpServer, IBeaconClientManager beaconClientManager, IGossipSubManager gossipSubManager, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public CancellationTokenSource? CancellationTokenSource { get; private set; }
    
    public ISyncProtocol SyncProtocol => syncProtocol;
    
    public async Task InitAsync()
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

            if (options.GossipSubEnabled)
            {
                gossipSubManager.Init();

                if (gossipSubManager.LightClientFinalityUpdate == null && gossipSubManager.LightClientOptimisticUpdate == null)
                {
                    return;
                }
            }
            
            await beaconClientManager.InitAsync();
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
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            
            if(options.GossipSubEnabled)
            {
                gossipSubManager.Start(token);
            }

            httpServer.Start();
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
        if (CancellationTokenSource == null)
        {
            _logger.LogWarning("Beacon client is not running. Nothing to stop");
            return;
        }
        
        await CancellationTokenSource.CancelAsync();
        
        if(options.GossipSubEnabled)
        {
            await gossipSubManager.StopAsync();
        }
        
        httpServer.Stop();
        await beaconClientManager.StopAsync();
        
        liteDbService.Dispose();
        CancellationTokenSource.Dispose();
        CancellationTokenSource = null;
    }
}