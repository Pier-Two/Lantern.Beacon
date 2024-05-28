using Lantern.Beacon.Sync.Types.Altair;

namespace Lantern.Beacon.Sync;

public interface ISyncProtocol
{
    Task InitAsync();
    
    Task StartAsync(CancellationToken token);
    
    Task StopAsync();
}