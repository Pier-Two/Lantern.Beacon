using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public interface IPeerManager
{ 
    ILocalPeer? LocalPeer { get; }
    
    Task InitAsync(CancellationToken token = default);
    
    Task StartAsync(CancellationToken token = default);
    
    Task StopAsync();
}