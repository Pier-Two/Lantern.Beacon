namespace Lantern.Beacon;

public class BeaconClientOptions
{
    public int MaxParallelDials { get; set; } = 5;
    
    public int DialTimeoutSeconds { get; set; } = 5;
    
    public int TcpPort { get; set; } = 9001;
}