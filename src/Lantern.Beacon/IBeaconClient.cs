using Lantern.Beacon.Sync;

namespace Lantern.Beacon;

public interface IBeaconClient
{
    ISyncProtocol SyncProtocol { get; }
    
    Task InitAsync();
    
    Task StartAsync(CancellationToken token = default);
    
    Task StopAsync();
}