using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public class CustomDiscoveryProtocol(BeaconClientOptions beaconOptions, SyncProtocolOptions syncProtocolOptions, IDiscv5Protocol discv5Protocol, IIdentityManager identityManager, ILoggerFactory loggerFactory) : ICustomDiscoveryProtocol
{
    private readonly ILogger<CustomDiscoveryProtocol> _logger = loggerFactory.CreateLogger<CustomDiscoveryProtocol>();
    
    public async Task<bool> InitAsync()
    {
        var result = await discv5Protocol.InitAsync();

        if (!result)
        {
            return false;
        }
        
        var currentVersion = Phase0Helpers.ComputeForkVersion(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocolOptions.GenesisTime)));
        var forkDigest = Phase0Helpers.ComputeForkDigest(currentVersion, syncProtocolOptions);
        var enrForkId = EnrForkId.Serialize(EnrForkId.CreateFrom(forkDigest, currentVersion, Constants.FarFutureEpoch), syncProtocolOptions.Preset);
        
        identityManager.Record.UpdateEntry(new EntryTcp(beaconOptions.TcpPort));
        identityManager.Record.UpdateEntry(new EntryEth2(enrForkId));

        _logger.LogInformation("Local ENR => {Enr}", identityManager.Record);
        
        return true;
    }
    
    public Task DiscoverAsync(Multiaddress localPeerAddr, CancellationToken token = default) => Task.CompletedTask;
    
    public async Task GetDiscoveredNodesAsync(Multiaddress localPeerAddr, CancellationToken token = default)
    {
        _logger.LogDebug("Starting discovery with local peer address: {LocalPeerAddr}", localPeerAddr);

        try
        {
            var nodes = discv5Protocol.GetAllNodes.ToArray();
            var discoveredNodes = new List<IEnr?>();
            var randomNodeId = new byte[32];
            
            Random.Shared.NextBytes(randomNodeId);
            var multiaddresses = new List<Multiaddress>();
            
            for (var i = 0; i < nodes.Length; i += beaconOptions.MaxParallelDials)
            {
                if (multiaddresses.Count >= beaconOptions.TargetNodesToFind)
                {
                    break;
                }
                
                _logger.LogInformation("Discovered {Count} peers so far...", multiaddresses.Count);
                
                var batchNodes = nodes.Skip(i).Take(beaconOptions.MaxParallelDials).ToList();
                var tasks = batchNodes.Select(node => discv5Protocol.SendFindNodeAsync(node, randomNodeId)).ToList();
                var nodesResponses = await Task.WhenAll(tasks);
                
                foreach (var nodesResponse in nodesResponses)
                {
                    if (nodesResponse == null)
                    {
                        continue;
                    }
            
                    discoveredNodes.AddRange(nodesResponse);
                    
                    foreach (var discoveredNode in discoveredNodes)
                    {
                        if (discoveredNode == null)
                            continue;
            
                        if (!(discoveredNode.HasKey(EnrEntryKey.Tcp) || 
                              discoveredNode.HasKey(EnrEntryKey.Tcp6)) || 
                              !discoveredNode.HasKey(EnrEntryKey.Eth2))
                            continue;
            
                        if (!discoveredNode.GetEntry<EntryEth2>(EnrEntryKey.Eth2).Value[..4]
                            .SequenceEqual(BeaconClientUtility.GetForkDigestBytes(syncProtocolOptions)))
                            continue;
            
                        var multiAddress = BeaconClientUtility.ConvertToMultiAddress(discoveredNode);
                        if (multiAddress == null)
                            continue;
            
                        multiaddresses.Add(multiAddress);
            
                        if (multiaddresses.Count >= beaconOptions.TargetNodesToFind)
                        {
                            break;
                        }
                    }
                    
                    if (multiaddresses.Count >= beaconOptions.TargetNodesToFind)
                    {
                        break;
                    }
                    
                    discoveredNodes.Clear();
                }
            }

            if (multiaddresses.Count != 0)
            {
                OnAddPeer?.Invoke(multiaddresses.ToArray());
                _logger.LogInformation("Discovery completed. Found {Count} peers for dialing", multiaddresses.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during discovery");
        }
    }
    
    public async Task StopAsync()
    {
        await discv5Protocol.StopAsync();
    }
    
    public Func<Multiaddress[], bool>? OnAddPeer { get; set; }
    
    public Func<Multiaddress[], bool>? OnRemovePeer { get; set; }
}