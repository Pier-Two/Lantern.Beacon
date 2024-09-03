namespace Lantern.Beacon;

public class BeaconClientOptions
{
    public int TargetPeerCount { get; set; } = 1;
    
    public int TargetNodesToFind { get; set; } = 100;
    
    public int MaxParallelDials { get; set; } = 1;
    
    public int DialTimeoutSeconds { get; set; } = 5;
    
    public int TcpPort { get; set; } = 9001;
    
    public bool GossipSubEnabled { get; set; } = true;
    
    public string DataDirectoryPath { get; set; } = 
        Path.Combine(
            Environment.OSVersion.Platform == PlatformID.Win32NT
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lantern", "lantern.db")
                : Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? string.Empty, ".lantern", "lantern.db")
        );
    
    public string[] Bootnodes { get; set; } = [];
    
    public bool EnableDiscovery { get; set; } = true;
}