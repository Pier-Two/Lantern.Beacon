using System.Text;
using IronSnappy;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types;
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
            InitialiseSyncProtocol();
            peerState.Init(peerFactoryBuilder.AppLayerProtocols);
            gossipSubManager.Init();

            if (gossipSubManager.LightClientFinalityUpdate == null || gossipSubManager.LightClientOptimisticUpdate == null)
            {
                return;
            }
            
            gossipSubManager.LightClientFinalityUpdate.OnMessage += HandleLightClientFinalityUpdate;
            gossipSubManager.LightClientOptimisticUpdate.OnMessage += HandleLightClientOptimisticUpdate;
            
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
    
    private void HandleLightClientFinalityUpdate(byte[] update)
    {
        var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
        var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
        var decompressedData = Snappy.Decode(update);
        var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
        var lightClientFinalityUpdate = DenebLightClientFinalityUpdate.Deserialize(decompressedData, syncProtocol.Options.Preset);

        _logger.LogInformation("Received light client finality update from gossip");

        if (denebFinalizedPeriod + 1 < denebCurrentPeriod) 
            return;
        
        var oldFinalizedHeader = syncProtocol.DenebLightClientStore.FinalizedHeader;
        var result = DenebProcessors.ProcessLightClientFinalityUpdate(syncProtocol.DenebLightClientStore, lightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
        _logger.LogInformation("Processed light client finality update from gossip");

        if (result)
        {
            if (!DenebHelpers.ShouldForwardFinalizedLightClientUpdate(lightClientFinalityUpdate, oldFinalizedHeader, syncProtocol))
                return;
            
            gossipSubManager.LightClientFinalityUpdate!.Publish(update);
            _logger.LogInformation("Forwarded light client finality update to gossip");
            
            syncProtocol.CurrentLightClientFinalityUpdate = lightClientFinalityUpdate;
            liteDbService.Store(nameof(DenebLightClientFinalityUpdate), lightClientFinalityUpdate);
            liteDbService.StoreOrUpdate(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
        }
        else
        {
            _logger.LogWarning("Failed to process light client finality update from gossip. Ignoring...");
        }
    }
    
    private void HandleLightClientOptimisticUpdate(byte[] update)
    {
        var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
        var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
        var decompressedData = Snappy.Decode(update);
        var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
        var lightClientOptimisticUpdate = DenebLightClientOptimisticUpdate.Deserialize(decompressedData, syncProtocol.Options.Preset);
        
        _logger.LogInformation("Received light client optimistic update from gossip");

        if (denebFinalizedPeriod + 1 < denebCurrentPeriod) 
            return;
        
        var oldOptimisticHeader = syncProtocol.DenebLightClientStore.OptimisticHeader;
        var result = DenebProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.DenebLightClientStore, lightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);

        if (result)
        {
            _logger.LogInformation("Processed light client optimistic update from gossip");
            if (!DenebHelpers.ShouldForwardLightClientOptimisticUpdate(lightClientOptimisticUpdate, oldOptimisticHeader, syncProtocol))
                return;
            
            gossipSubManager.LightClientFinalityUpdate!.Publish(update);
            _logger.LogInformation("Forwarded light client optimistic update to gossip");
                
            syncProtocol.CurrentLightClientOptimisticUpdate = lightClientOptimisticUpdate;
            liteDbService.Store(nameof(DenebLightClientOptimisticUpdate), lightClientOptimisticUpdate); 
            liteDbService.StoreOrUpdate(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
        }
        else
        {
            _logger.LogWarning("Failed to process light client optimistic update from gossip. Ignoring...");
        }
    }

    private void InitialiseSyncProtocol()
    {
        var altairStore = liteDbService.Fetch<AltairLightClientStore>(nameof(AltairLightClientStore));
        var capellaStore = liteDbService.Fetch<CapellaLightClientStore>(nameof(CapellaLightClientStore));
        var denebStore = liteDbService.Fetch<DenebLightClientStore>(nameof(DenebLightClientStore));
        var finalityUpdate = liteDbService.Fetch<DenebLightClientFinalityUpdate>(nameof(DenebLightClientFinalityUpdate));
        var optimisticUpdate = liteDbService.Fetch<DenebLightClientOptimisticUpdate>(nameof(DenebLightClientOptimisticUpdate));
        
        syncProtocol.Init(altairStore, capellaStore, denebStore, finalityUpdate, optimisticUpdate);
    }
}