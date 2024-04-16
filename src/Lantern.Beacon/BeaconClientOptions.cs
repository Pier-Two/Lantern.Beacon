namespace Lantern.Beacon;

public class BeaconClientOptions
{
    public int RefreshPeersInterval { get; set; } = 10;
    
    public int DialTimeout { get; set; } = 10;
    
    public int TcpPort { get; set; } = 9001;
}