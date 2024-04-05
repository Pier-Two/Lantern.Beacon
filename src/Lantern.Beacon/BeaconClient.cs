using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon;

public class BeaconClient( IDiscoveryProtocol discoveryProtocol, IPeerManager peerManager, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public async Task InitAsync()
    {
        try
        {
            await discoveryProtocol.InitAsync();
            await peerManager.InitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start discovery protocol");
            throw;
        }
    }
    
    public async Task StartAsync()
    {
        try
        {
           var nodes = await discoveryProtocol.DiscoverAsync();

           foreach (var node in nodes)
           {
               Console.WriteLine($"Discovered node: {node}");
           }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Beacon client");
            throw;
        }
    }
    
    public async Task StopAsync()
    {
        await discoveryProtocol.StopAsync();
    }
}