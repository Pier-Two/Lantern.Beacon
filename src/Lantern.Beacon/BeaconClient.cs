using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon;

public class BeaconClient(IDiscoveryProtocol discoveryProtocol, IPeerManager peerManager, ISyncProtocol syncProtocol, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public async Task InitAsync(CancellationToken token = default)
    {
        try
        {
            await syncProtocol.InitAsync();
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
            var syncTask = syncProtocol.StartAsync(token); 
            await peerManager.StartAsync(token); 
            await syncTask; 
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Beacon client");
            throw;
        }
    }
    
    public async Task StopAsync()
    {
        await syncProtocol.StopAsync();
        await peerManager.StopAsync();
    }
}