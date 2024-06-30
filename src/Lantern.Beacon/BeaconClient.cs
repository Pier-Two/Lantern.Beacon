using System.Text;
using IronSnappy;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types.Deneb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClient(IPeerFactoryBuilder peerFactoryBuilder, ISyncProtocol syncProtocol, IBeaconClientManager beaconClientManager, IGossipSubManager gossipSubManager, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public async Task InitAsync(CancellationToken token = default)
    {
        try
        {
            syncProtocol.AppLayerProtocols = peerFactoryBuilder.AppLayerProtocols;
            syncProtocol.Init();
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
    }
    
    private void HandleLightClientFinalityUpdate(byte[] update)
    {
        var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(
                syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
        var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
        var decompressedData = Snappy.Decode(update);
        var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
        var lightClientFinalityUpdate = DenebLightClientFinalityUpdate.Deserialize(decompressedData, syncProtocol.Options.Preset);

        _logger.LogInformation("Received light client finality update from gossip");
        
        if (denebFinalizedPeriod + 1 >= denebCurrentPeriod)
        {
            DenebProcessors.ProcessLightClientFinalityUpdate(syncProtocol.DenebLightClientStore, lightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
            _logger.LogInformation("Processed light client finality update from gossip");
        }
        
        // Add validations and processing logic here before publishing to the network 
    }
    
    private void HandleLightClientOptimisticUpdate(byte[] update)
    {
        var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(
                syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
        var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
        var decompressedData = Snappy.Decode(update);
        var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
        var lightClientOptimisticUpdate = DenebLightClientOptimisticUpdate.Deserialize(decompressedData, syncProtocol.Options.Preset);

        _logger.LogInformation("Received light client optimistic update from gossip for head block {BlockRoot}",  Convert.ToHexString(lightClientOptimisticUpdate.AttestedHeader.GetHashTreeRoot(syncProtocol.Options.Preset)));
        
        if (denebFinalizedPeriod + 1 >= denebCurrentPeriod)
        {
            DenebProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.DenebLightClientStore, lightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
            _logger.LogInformation("Processed light client optimistic update from gossip");
        }
        // Add validations and processing logic here before publishing to the network 
    }
}