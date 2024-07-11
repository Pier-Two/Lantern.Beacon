namespace Lantern.Beacon;

public class BeaconClientOptions
{
    public int TargetPeerCount { get; set; } = 1;
    
    public int MaxParallelDials { get; set; } = 5;
    
    public int DialTimeoutSeconds { get; set; } = 5;
    
    public int TcpPort { get; set; } = 9001;
    
    public string DataDirectoryPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lantern");
    
    public string[] Bootnodes { get; set; } = [];
    
    public bool EnableDiscovery { get; set; } = true;
    
    public bool PersistData { get; set; } = true;
}