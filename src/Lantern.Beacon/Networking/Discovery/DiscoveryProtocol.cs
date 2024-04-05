using Lantern.Beacon.Utility;
using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public class DiscoveryProtocol(IDiscv5Protocol discv5Protocol) : IDiscoveryProtocol
{
    public IEnr SelfEnr => discv5Protocol.SelfEnr;
    
    public IEnumerable<IEnr> ActiveNodes => discv5Protocol.GetActiveNodes;

    public async Task InitAsync()
    {
        await discv5Protocol.InitAsync();
    }

    public async Task StopAsync()
    {
        await discv5Protocol.StopAsync();
    }
    
    public async Task<IEnumerable<Multiaddress?>> DiscoverAsync(CancellationToken token = default)
    {
        var randomId = new byte[32];
        
        Random.Shared.NextBytes(randomId);
        
        var peers = await discv5Protocol.PerformLookupAsync(randomId);
        
        return peers == null ? Enumerable.Empty<Multiaddress>() : peers.Select(MultiAddressEnrConverter.ConvertToMultiAddress);
    }


}