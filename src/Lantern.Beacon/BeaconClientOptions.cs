using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types;
using Microsoft.Extensions.Logging;
using SszSharp;

namespace Lantern.Beacon;

public class BeaconClientOptions
{
    public int TargetPeerCount { get; set; } = 1;
    
    public int TargetNodesToFind { get; set; } = 100;
    
    public int MaxParallelDials { get; set; } = 10;
    
    public int DialTimeoutSeconds { get; set; } = 10;
    
    public int TcpPort { get; set; } = 9001;
    
    public bool GossipSubEnabled { get; set; } = true;
    
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
    
    public static BeaconClientOptions Parse(string[] args)
    {
        var argsList = args.ToList();
        var options = new BeaconClientOptions();
    
        for (var i = 0; i < argsList.Count; i++)
        {
            var arg = argsList[i].ToLowerInvariant();
            
            switch (arg)
            {
                case "--network":
                    if (i + 1 < argsList.Count)
                    {
                        options.SyncProtocolOptions.Network = GetNetworkType(argsList[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for --network");
                    }
                    break;
                case "--log-level":
                    if (i + 1 < args.Length)
                    {
                        var levelString = args[++i];
                        if (Enum.TryParse<LogLevel>(levelString, true, out var logLevel))
                        {
                            options.LogLevel = logLevel;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid log level: {levelString}");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for --log-level");
                    }
                    break;
                case "--genesis-time":
                    if (i + 1 < argsList.Count && ulong.TryParse(argsList[++i], out var genesisTime))
                    {
                        options.SyncProtocolOptions.GenesisTime = genesisTime;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --genesis-time");
                    }
                    break;
                
                case "--genesis-validators-root":
                    if (i + 1 < argsList.Count)
                    {
                        options.SyncProtocolOptions.GenesisValidatorsRoot = GetTrustedBlockRootBytes(argsList[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for --genesis-validators-root");
                    }
                    break;
                
                case "--preset":
                    if (i + 1 < argsList.Count)
                    {
                        options.SyncProtocolOptions.Preset = GetPreset(argsList[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for --preset");
                    }
                    break;
                
                case "--block-root":
                    if (i + 1 < argsList.Count)
                    {
                        options.SyncProtocolOptions.TrustedBlockRoot = GetTrustedBlockRootBytes(argsList[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for --block-root");
                    }
                    break;

                case "--datadir":
                    if (i + 1 < argsList.Count)
                    {
                        var providedPath = argsList[++i];
                        options.DataDirectoryPath = Path.Combine(providedPath, "lantern", "lantern.db");
                    }
                    else
                    {
                        throw new ArgumentException("Missing value for --datadir");
                    }
                    break;

                case "--peer-count":
                    if (i + 1 < argsList.Count && int.TryParse(argsList[++i], out int peerCount))
                    {
                        options.TargetPeerCount = peerCount;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --peer-count");
                    }
                    break;

                case "--discovery-peer-count":
                    if (i + 1 < argsList.Count && int.TryParse(argsList[++i], out var discoveryPeerCount))
                    {
                        options.TargetNodesToFind = discoveryPeerCount;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --discovery-peer-count");
                    }
                    break;

                case "--dial-parallelism":
                    if (i + 1 < argsList.Count && int.TryParse(argsList[++i], out var dialParallelism))
                    {
                        options.MaxParallelDials = dialParallelism;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --dial-parallelism");
                    }
                    break;

                case "--dial-timeout":
                    if (i + 1 < argsList.Count && int.TryParse(argsList[++i], out var dialTimeout))
                    {
                        options.DialTimeoutSeconds = dialTimeout;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --dial-timeout");
                    }
                    break;

                case "--tcp-port":
                    if (i + 1 < argsList.Count && int.TryParse(argsList[++i], out var tcpPort))
                    {
                        options.TcpPort = tcpPort;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --tcp-port");
                    }
                    break;

                case "--gossip-sub-enabled":
                    if (i + 1 < argsList.Count && bool.TryParse(argsList[++i], out var gossipEnabled))
                    {
                        options.GossipSubEnabled = gossipEnabled;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --gossip-sub-enabled");
                    }
                    break;

                case "--bootnodes":
                    while (i + 1 < argsList.Count && !argsList[i + 1].StartsWith("--"))
                    {
                        options.Bootnodes.Add(argsList[++i]);
                    }
                    break;
                
                case "--enable-discovery":
                    if (i + 1 < argsList.Count && bool.TryParse(argsList[++i], out var enableDiscovery))
                    {
                        options.EnableDiscovery = enableDiscovery;
                    }
                    else
                    {
                        throw new ArgumentException("Invalid or missing value for --enable-discovery");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {argsList[i]}");
            }
        }

        return options;
    }
    
    private static SizePreset GetPreset(string preset)
    {
        return preset.ToLower() switch
        {
            "mainnet" => SizePreset.MainnetPreset,
            "holesky" => SizePreset.MainnetPreset,
            _ => throw new ArgumentException($"Unsupported preset: {preset}")
        };
    }
    
    private static NetworkType GetNetworkType(string network)
    {
        return network.ToLower() switch
        {
            "mainnet" => NetworkType.Mainnet,
            "holesky" => NetworkType.Holesky,
            _ => throw new ArgumentException($"Unsupported network type: {network}")
        };
    }
    
    private static byte[] GetTrustedBlockRootBytes(string trustedBlockRoot)
    {
        try
        {
            if(trustedBlockRoot.StartsWith("0x"))
            {
                trustedBlockRoot = trustedBlockRoot[2..];
            }
            
            return Convert.FromHexString(trustedBlockRoot);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid hex string for --block-root");
        }
    }
}