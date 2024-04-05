using Lantern.Discv5.Enr;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public interface IDiscoveryProtocol
{
    IEnr SelfEnr { get; }
    
    IEnumerable<IEnr> ActiveNodes { get; }
    
    Task InitAsync();
    
    Task StopAsync();
    
    Task<IEnumerable<Multiaddress?>> DiscoverAsync(CancellationToken token = default);
}