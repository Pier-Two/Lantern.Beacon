using Lantern.Beacon.Utility;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public class DiscoveryProtocol(IDiscv5Protocol discv5Protocol, IIdentityManager identityManager, ILoggerFactory loggerFactory) : IDiscoveryProtocol
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
        
        var tcpPort = discv5Protocol.SelfEnr.GetEntry<EntryUdp>(EnrEntryKey.Udp).Value + 1;
        identityManager.Record.UpdateEntry(new EntryTcp(tcpPort));
        
        _logger.LogInformation("Self ENR updated => {Enr}", identityManager.Record);
        
        return true;
    }

    public async Task StopAsync()
    {
        await discv5Protocol.StopAsync();
    }
    
    public async Task<IEnumerable<Multiaddress?>> DiscoverAsync(byte[] nodeId,CancellationToken token = default)
    {
        var peers = await discv5Protocol.DiscoverAsync(nodeId);
        
        return peers == null ? Enumerable.Empty<Multiaddress>() : peers.Select(MultiAddressEnrConverter.ConvertToMultiAddress);
    }


}