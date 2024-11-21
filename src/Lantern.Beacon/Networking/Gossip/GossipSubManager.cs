using IronSnappy;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.Gossip.Topics;
using Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using ITopic = Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.ITopic;
using Settings = Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.Settings;

namespace Lantern.Beacon.Networking.Gossip;

public class GossipSubManager(ManualDiscoveryProtocol discoveryProtocol, SyncProtocolOptions syncProtocolOptions, CustomPubsubRouter router, IPeerState peerState, IBeaconClientManager beaconClientManager, ISyncProtocol syncProtocol, ILiteDbService liteDbService, ILoggerFactory loggerFactory) : IGossipSubManager
{
    private readonly ILogger<GossipSubManager> _logger = loggerFactory.CreateLogger<GossipSubManager>();
    private CancellationTokenSource? _cancellationTokenSource;
    
    public ITopic? LightClientFinalityUpdate { get; private set; }
    public ITopic? LightClientOptimisticUpdate { get; private set; }
    
    public void Init()
    {
        LightClientFinalityUpdate = router.Subscribe(LightClientFinalityUpdateTopic.GetTopicString(syncProtocolOptions));
        LightClientOptimisticUpdate = router.Subscribe(LightClientOptimisticUpdateTopic.GetTopicString(syncProtocolOptions));
        LightClientFinalityUpdate.OnMessage += HandleLightClientFinalityUpdate;
        LightClientOptimisticUpdate.OnMessage += HandleLightClientOptimisticUpdate;

        _logger.LogDebug("Subscribed to topic: {LightClientFinalityUpdate}", LightClientFinalityUpdateTopic.GetTopicString(syncProtocolOptions));
        _logger.LogDebug("Subscribed to topic: {LightClientOptimisticUpdate}", LightClientOptimisticUpdateTopic.GetTopicString(syncProtocolOptions));
    }
    
    public void Start(CancellationToken token = default)
    {
        if(beaconClientManager.LocalPeer == null)
        {
            throw new Exception("Local peer is not initialized. Cannot start gossip sub protocol");
        }
        
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        var settings = new Settings
        {
            Degree = GossipSubSettings.Degree,
            LowestDegree = GossipSubSettings.LowestDegree,
            HighestDegree = GossipSubSettings.HighestDegree,
            LazyDegree = GossipSubSettings.LazyDegree,
            HeartbeatInterval = GossipSubSettings.HeartbeatInterval,
            FanoutTtl = GossipSubSettings.FanoutTtl,
            mcache_len = GossipSubSettings.MCacheLen,
            mcache_gossip = GossipSubSettings.MCacheGossip,
            MessageCacheTtl = GossipSubSettings.MessageCacheTtl,
            DefaultSignaturePolicy = GossipSubSettings.DefaultSignaturePolicy
        };
        
        _ = Task.Run(() => router.RunAsync(beaconClientManager.LocalPeer, discoveryProtocol, settings, token), token);
        _logger.LogDebug("Running GossipSub protocol");
    }
    
    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }
        
        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }
    
    private void HandleLightClientFinalityUpdate(PeerId peerId, byte[] update)
    {
        try
        {
            CheckIfPeerExistsInGossip(peerId);
            
            _logger.LogInformation("Received light client finality update from gossip peer {PeerId}", peerId);
            
            var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
                Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
            var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
                Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            var decompressedData = Snappy.Decode(update);
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
            var lightClientFinalityUpdate =
                DenebLightClientFinalityUpdate.Deserialize(decompressedData, syncProtocol.Options.Preset);

            if (denebFinalizedPeriod + 1 < denebCurrentPeriod)
                return;

            var oldFinalizedHeader = syncProtocol.DenebLightClientStore.FinalizedHeader;
            var result = DenebProcessors.ProcessLightClientFinalityUpdate(syncProtocol.DenebLightClientStore,
                lightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);

            if (result)
            {
                _logger.LogInformation("Processed light client finality update from gossip peer {PeerId}", peerId);
                
                if (!DenebHelpers.ShouldForwardFinalizedLightClientUpdate(lightClientFinalityUpdate, oldFinalizedHeader,
                        syncProtocol))
                    return;

                LightClientFinalityUpdate!.Publish(update);
                _logger.LogDebug("Forwarded light client finality update to gossip");

                syncProtocol.CurrentLightClientFinalityUpdate = lightClientFinalityUpdate;
                liteDbService.Store(nameof(DenebLightClientFinalityUpdate), lightClientFinalityUpdate);
                liteDbService.ReplaceAllWithItem(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
            }
            else
            {
                _logger.LogWarning("Failed to process light client finality update from gossip. Ignoring...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process light client finality update from gossip");
        }
    }
    
    private void HandleLightClientOptimisticUpdate(PeerId peerId, byte[] update)
    {
        try
        {
            CheckIfPeerExistsInGossip(peerId);
            
            _logger.LogInformation("Received light client optimistic update from gossip peer {PeerId}", peerId);
            
            var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
            var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            var decompressedData = Snappy.Decode(update);
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
            var lightClientOptimisticUpdate = DenebLightClientOptimisticUpdate.Deserialize(decompressedData, syncProtocol.Options.Preset);
            
            if (denebFinalizedPeriod + 1 < denebCurrentPeriod) 
                return;
            
            var oldOptimisticHeader = syncProtocol.DenebLightClientStore.OptimisticHeader;
            var result = DenebProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.DenebLightClientStore, lightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);

            if (result)
            {
                _logger.LogInformation("Processed light client optimistic update from gossip peer {PeerId}", peerId);
                
                if (!DenebHelpers.ShouldForwardLightClientOptimisticUpdate(lightClientOptimisticUpdate, oldOptimisticHeader, syncProtocol))
                    return;
                
                LightClientOptimisticUpdate!.Publish(update);
                _logger.LogDebug("Forwarded light client optimistic update to gossip");
                    
                syncProtocol.CurrentLightClientOptimisticUpdate = lightClientOptimisticUpdate;
                liteDbService.Store(nameof(DenebLightClientOptimisticUpdate), lightClientOptimisticUpdate); 
                liteDbService.ReplaceAllWithItem(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
            }
            else
            {
                _logger.LogWarning("Failed to process light client optimistic update from gossip. Ignoring...");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process light client optimistic update from gossip");
        }
    }
    
    private void CheckIfPeerExistsInGossip(PeerId peerId)
    {
        if (!syncProtocol.IsInitialised)
        {
            _logger.LogDebug("Sync protocol is not initialized. Ignore gossip message");
            return;
        }
        
        var foundLivePeer = false;
                
        foreach (var gossipPeer in peerState.GossipPeers.Keys)
        {
            _logger?.LogDebug("Checking if peer {PeerId} matches ID {Id}", gossipPeer, peerId);
                
            if (!gossipPeer.Bytes.SequenceEqual(peerId.Bytes))
                continue;
                
            _logger?.LogDebug("Peer {PeerId} already exists in 'GossipPeers'", gossipPeer);
            foundLivePeer = true;
            break; 
        }

        if (foundLivePeer) 
            return;
        
        _logger?.LogInformation("Adding peer {PeerId} to GossipPeers", peerId);
        peerState.GossipPeers.TryAdd(peerId, true);
    }
}