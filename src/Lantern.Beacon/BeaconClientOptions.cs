using Lantern.Beacon.Sync;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Dto;

namespace Lantern.Beacon;

public class BeaconClientOptions
{
    public int TargetPeerCount { get; set; } = 1;
    
    public int TargetNodesToFind { get; set; } = 100;
    
    public int MaxParallelDials { get; set; } = 10;
    
    public int DialTimeoutSeconds { get; set; } = 10;
    
    public int TcpPort { get; set; } = 9001;
    
    public int HttpPort { get; set; } = 5052;
    
    public bool GossipSubEnabled { get; set; } = true;

    public Identity Identity { get; init; } = new(null, KeyType.Secp256K1);
    
    public LogLevel LogLevel { get; set; } = LogLevel.Information; 
    
    public string DataDirectoryPath { get; set; } = 
        Path.Combine(
            Environment.OSVersion.Platform == PlatformID.Win32NT 
                ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                : Environment.GetEnvironmentVariable("HOME") ?? string.Empty
        );
    
    public List<string> Bootnodes { get; set; } = [];
    
    public bool EnableDiscovery { get; set; } = true;
    
    public SyncProtocolOptions SyncProtocolOptions { get; set; } = new();
}