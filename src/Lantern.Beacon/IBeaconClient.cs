namespace Lantern.Beacon;

public interface IBeaconClient
{
    Task InitAsync();
    
    Task StartAsync();
}