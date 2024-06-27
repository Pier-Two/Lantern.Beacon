using System.Text;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon;

public class BeaconClient(ISyncProtocol syncProtocol, IBeaconClientManager beaconClientManager, IGossipSubManager gossipSubManager, IServiceProvider serviceProvider) : IBeaconClient
{
    private readonly ILogger<BeaconClient> _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BeaconClient>();
    
    public async Task InitAsync(CancellationToken token = default)
    {
        try
        {
            syncProtocol.Init();
            gossipSubManager.Init();

            if (gossipSubManager.BeaconBlock == null || gossipSubManager.LightClientFinalityUpdate == null || gossipSubManager.LightClientOptimisticUpdate == null)
            {
                return;
            }
            
            gossipSubManager.BeaconBlock.OnMessage += ProcessBeaconBlock;
            await beaconClientManager.InitAsync(token);
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
            //await gossipSubManager.StartAsync(token);
            await beaconClientManager.StartAsync(token); 
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start Beacon client");
            throw;
        }
    }
    
    public async Task StopAsync()
    {
        //await gossipSubManager.StopAsync();
        await beaconClientManager.StopAsync();
    }
    
    private void ProcessBeaconBlock(byte[] obj)
    {
        //Console.WriteLine("Received message: " + Encoding.UTF8.GetString(obj));
    }
}