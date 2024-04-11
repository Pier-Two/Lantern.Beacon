using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon;

public class BeaconClient( IDiscoveryProtocol discoveryProtocol, IPeerManager peerManager, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public async Task InitAsync(CancellationToken token = default)
    {
        try
        {
            await peerManager.InitAsync(token);
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
           await peerManager.StartAsync(token);
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