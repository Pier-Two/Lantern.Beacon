namespace Lantern.Beacon;

public interface IBeaconClient
{
    Task InitAsync(CancellationToken token = default);
    
    Task StartAsync(CancellationToken token = default);
}