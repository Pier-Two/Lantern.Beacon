using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.Gossip.Topics;
using Lantern.Beacon.Sync;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Protocols.Pubsub;

namespace Lantern.Beacon.Networking.Gossip;

public class GossipSubManager(ManualDiscoveryProtocol discoveryProtocol, SyncProtocolOptions syncProtocolOptions, PubsubRouter router, IBeaconClientManager beaconClientManager, ILoggerFactory loggerFactory) : IGossipSubManager
{
    private readonly ILogger<GossipSubManager> _logger = loggerFactory.CreateLogger<GossipSubManager>();
    private CancellationTokenSource? _cancellationTokenSource;
    
    public ITopic? LightClientFinalityUpdate { get; private set; }
    public ITopic? LightClientOptimisticUpdate { get; private set; }
    
    public void Init()
    {
        LightClientFinalityUpdate = router.Subscribe(LightClientFinalityUpdateTopic.GetTopicString(syncProtocolOptions));
        LightClientOptimisticUpdate = router.Subscribe(LightClientOptimisticUpdateTopic.GetTopicString(syncProtocolOptions));
        
        _logger.LogDebug("Subscribed to topic: {LightClientFinalityUpdate}", LightClientFinalityUpdateTopic.GetTopicString(syncProtocolOptions));
        _logger.LogDebug("Subscribed to topic: {LightClientOptimisticUpdate}", LightClientOptimisticUpdateTopic.GetTopicString(syncProtocolOptions));
    }
    
    public Task StartAsync(CancellationToken token = default)
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
        _logger.LogInformation("Running GossipSub protocol");
      
        return Task.CompletedTask;
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
}