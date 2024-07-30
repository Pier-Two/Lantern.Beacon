using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public interface IBeaconClientManager
{ 
    CancellationTokenSource? CancellationTokenSource { get; }
    
    ILocalPeer? LocalPeer { get; }
    
    Task InitAsync(CancellationToken token = default);
    
    Task StartAsync(CancellationToken token = default);
    
    Task StopAsync();
}