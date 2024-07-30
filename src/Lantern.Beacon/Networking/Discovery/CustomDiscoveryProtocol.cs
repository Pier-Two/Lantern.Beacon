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
        
        identityManager.Record.UpdateEntry(new EntryTcp(beaconOptions.TcpPort));

        var currentVersion = Phase0Helpers.ComputeForkVersion(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocolOptions.GenesisTime)));
        var forkDigest = Phase0Helpers.ComputeForkDigest(currentVersion, syncProtocolOptions);
        var enrForkId = EnrForkId.Serialize(EnrForkId.CreateFrom(forkDigest, currentVersion, Constants.FarFutureEpoch), syncProtocolOptions.Preset);
        
        identityManager.Record.UpdateEntry(new EntryEth2(enrForkId));

        _logger.LogInformation("Self ENR updated => {Enr}", identityManager.Record);
        
        return true;
    }
    
    public Task DiscoverAsync(Multiaddress localPeerAddr, CancellationToken token = default) => Task.CompletedTask;

    public async Task GetDiscoveredNodesAsync(Multiaddress localPeerAddr, CancellationToken token = default)
    {
        _logger.LogInformation("Starting discovery with local peer address: {LocalPeerAddr}", localPeerAddr);

        try
        {
            var nodes = discv5Protocol.GetActiveNodes;
            
            if(!discv5Protocol.GetActiveNodes.Any())
            {
                _logger.LogInformation("No active nodes found. Skipping discovery");
                return;
            }
            
            var discoveredNodes = new List<IEnr?>();
            var randomNodeId = new byte[32];
            
            Random.Shared.NextBytes(randomNodeId);

            foreach (var node in nodes)
            {
                var nodesResponse = await discv5Protocol.SendFindNodeAsync(node, randomNodeId);
                
                if (nodesResponse == null)
                {
                    continue;
                }

                discoveredNodes.AddRange(nodesResponse);
            }
            
            var multiaddresses = new List<Multiaddress>();
                
            foreach (var node in discoveredNodes)
            {
                if(node == null)
                    continue;
                
                if (!(node.HasKey(EnrEntryKey.Tcp) || node.HasKey(EnrEntryKey.Tcp6)) || !node.HasKey(EnrEntryKey.Eth2))
                    continue;
 
                var multiAddress = BeaconClientUtility.ConvertToMultiAddress(node);
                    
                if(multiAddress == null)
                    continue;
                    
                multiaddresses.Add(multiAddress);  
            }

            if (multiaddresses.Count != 0)
            {
                OnAddPeer?.Invoke(multiaddresses.ToArray());
                _logger.LogInformation("Collected {Count} peers to dial", multiaddresses.Count);
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