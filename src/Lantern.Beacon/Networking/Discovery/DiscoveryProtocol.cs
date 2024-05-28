using Lantern.Beacon.Utility;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public class DiscoveryProtocol(BeaconClientOptions options, IDiscv5Protocol discv5Protocol, IIdentityManager identityManager, ILoggerFactory loggerFactory) : IDiscoveryProtocol
{
    private readonly ILogger<DiscoveryProtocol> _logger = loggerFactory.CreateLogger<DiscoveryProtocol>();
    public IEnr? SelfEnr => discv5Protocol.SelfEnr;
    public IEnumerable<IEnr> ActiveNodes => discv5Protocol.GetActiveNodes;

    public async Task<bool> InitAsync()
    {
        var result = await discv5Protocol.InitAsync();

        if (!result)
        {
            return false;
        }
        
        identityManager.Record.UpdateEntry(new EntryTcp(options.TcpPort));
        
        _logger.LogInformation("Self ENR updated => {Enr}", identityManager.Record);
        
        var randomId = new byte[32];
        Random.Shared.NextBytes(randomId);
        await discv5Protocol.DiscoverAsync(randomId);
        
        return true;
    }

    public async Task StopAsync()
    {
        await discv5Protocol.StopAsync();
    }
    
    public async Task<IEnumerable<Node?>> DiscoverAsync(byte[] nodeId, CancellationToken token = default)
    {
        var nodes = discv5Protocol.GetActiveNodes;
        List<Node>? peers = [];
      
        _logger.LogInformation("Discovering light client peers...");
        
        foreach (var node in nodes)
        {
            var discoveredPeers = await discv5Protocol.SendFindNodeAsync(node, nodeId);
            
            if (discoveredPeers == null)
            {
                continue;
            }
            
            peers.AddRange(discoveredPeers
                .Where(p => p.HasKey(EnrEntryKey.Tcp) || p.HasKey(EnrEntryKey.Tcp6))
                .Select(p => new Node(p)));
        }

        return peers == null ? Enumerable.Empty<Node>() : peers;
    }
}