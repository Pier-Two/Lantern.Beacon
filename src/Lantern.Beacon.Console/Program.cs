using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SszSharp;

namespace Lantern.Beacon.Console;

internal static class Program
{
    public static async Task Main()
    {
        var stopTokenSource = new CancellationTokenSource();
        var stopToken = stopTokenSource.Token;
        
        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; 
            System.Console.WriteLine("Ctrl+C pressed. Stopping the beacon client...");
            stopTokenSource.Cancel();
        };

        // Discv5 options
        var bootstrapEnrs = new[]
        {
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg",
            "enr:-Le4QPUXJS2BTORXxyx2Ia-9ae4YqA_JWX3ssj4E_J-3z1A-HmFGrU8BpvpqhNabayXeOZ2Nq_sbeDgtzMJpLLnXFgAChGV0aDKQtTA_KgEAAAAAIgEAAAAAAIJpZIJ2NIJpcISsaa0Zg2lwNpAkAIkHAAAAAPA8kv_-awoTiXNlY3AyNTZrMaEDHAD2JKYevx89W0CcFJFiskdcEzkH_Wdv9iW42qLK79ODdWRwgiMohHVkcDaCI4I"
        };
        var connectionOptions = new ConnectionOptions();
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions(bootstrapEnrs);
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        var discv5LoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));

        // Beacon client options
        var syncProtocolOptions = new SyncProtocolOptions
        {
            Preset = SizePreset.MainnetPreset,
            GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
            GenesisTime = 1606824023,
            TrustedBlockRoot = Convert.FromHexString("0aa4d679aa288c81b6513491b9bc1ad6c1faab3c727ebb4887458179df418616"),
            Network = NetworkType.Mainnet
        };
        var beaconClientOptions = new BeaconClientOptions
        {
            TcpPort = 9005,
            DialTimeoutSeconds = 20,
            MaxParallelDials = 10,
            EnableDiscovery = true,
            GossipSubEnabled = true,
            //Bootnodes = ["/ip4/94.16.205.215/tcp/9000/p2p/16Uiu2HAm9UL4C273yG9iBQS6RWavcxZDi5rGfSZXLvgd5vnDszCN"]
            //Bootnodes = ["/ip4/194.33.40.70/tcp/9002/p2p/16Uiu2HAmFytwaDJqRrXM4rNc7AKWeJjxWSjdtYRWTsfwZ7FzRA3m"]
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAm66XKpWs6y6pkGJAiDdxmrdhZG3JYrmmma32U5mNFcStY"]
            //Bootnodes = ["/ip4/72.219.149.222/tcp/9001/p2p/16Uiu2HAmK8s25hdUhLbmdssRSdCVgwpxo8XpYjh3hwqComxTkbes"]
            //Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAm7oPB47QN72JyAKv92ww6t7LkZpGuoeQ4nxPzSwBcFVqf"] // Internal OVH Nimbus Client
            //Bootnodes = ["/ip4/65.109.107.102/tcp/10001/p2p/16Uiu2HAkx9x6q9artkr11wXzvJ55Sv74c6SamAQmjmFDyd1hvYyu"] //Discovered Nimbus Client (Not synced)
            //Bootnodes = ["/ip4/194.33.40.72/tcp/9001/p2p/16Uiu2HAmKnikHhMXdEYzDqXDAUuqnjQNwmTZuWNVyo73aHePjgvq"] // Public Nimbus Full Client
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAmH5mNHiU3nepLqhUQ5gZvYK7EcJi5BYBV4XzUBuTcxGw4"] // Local Nimbus Full Client
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9014/p2p/16Uiu2HAm8fBXikjEJnzSvjwuSRozFacyLQQWg8ajHxqN3HGbs2zT"] // Local Lodestar Full Client
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAkvFPuyKCb1LS68v2ezaFk7uRPw72afm4F2TttEv8uJk32"] // Local Nimbus Light Client
            //Bootnodes = ["/ip4/69.175.102.62/tcp/31018/p2p/16Uiu2HAm2FWXMoKEsshxjXNsXmFwxPAm4eaWmcffFTGgNs3gi4Ww"], // Erigon
            //Bootnodes = ["/ip4/136.243.72.174/tcp/9000/p2p/16Uiu2HAkvTExpESzdQfYg6N3RtX6YNerBXTeg6vXzTMAws3tUQCA"],
            //Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAkwvVXtZj6u3R2F7hEXpnbDUom3rDepABdDCSzyzAM2k69"],
            //Bootnodes = ["/ip4/54.38.80.34/tcp/9000/p2p/16Uiu2HAm8t1aQArVwrJ9fwHRGXL2sXumPGTvmsne14piPaFJ5FYi"], // Lighthouse
            //Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAm1iCnKSNGhee2RKa1EYbazz4JJ8CDVVCPLyXS9PFYPG1A"] // Lodestar
        };
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddFilter("Nethermind.Libp2p.Core.ChannelFactory", LogLevel.None)
                .AddSimpleConsole(l => 
                {
                    l.SingleLine = true;
                    l.TimestampFormat = "[HH:mm:ss] ";
                    l.ColorBehavior = LoggerColorBehavior.Default;
                    l.IncludeScopes = false;
                    l.UseUtcTimestamp = true;
                });
        });
        var services = new ServiceCollection();
        
        // Setup services
        services.AddBeaconClient(beaconClientBuilder =>
        {
            beaconClientBuilder.AddDiscoveryProtocol(discv5Builder =>
            {
                discv5Builder
                    .WithConnectionOptions(connectionOptions)
                    .WithTableOptions(tableOptions)
                    .WithEnrBuilder(enr)
                    .WithSessionOptions(sessionOptions)
                    .WithLoggerFactory(discv5LoggerFactory);
            });

            beaconClientBuilder.WithBeaconClientOptions(beaconClientOptions);
            beaconClientBuilder.WithSyncProtocolOptions(syncProtocolOptions);
            beaconClientBuilder.AddLibp2pProtocol(libp2PBuilder => libp2PBuilder);
            beaconClientBuilder.WithLoggerFactory(loggerFactory);
        });

        var serviceProvider = services.BuildServiceProvider();
        var beaconClient = serviceProvider.GetRequiredService<IBeaconClient>();

        try
        {
            await beaconClient.InitAsync();
            await beaconClient.StartAsync(stopToken);
            
            await Task.Delay(-1, stopToken);
        }
        catch (OperationCanceledException)
        {
            
        }
        finally
        {
            await beaconClient.StopAsync();
            System.Console.WriteLine("Beacon client stopped.");
        }
    }
}

// var libp2p2LoggerFactory = LoggerFactory.Create(builder =>
// {
//     builder
//         .SetMinimumLevel(LogLevel.Information)
//         .AddProvider(new CustomConsoleLoggerProvider(
//             config => config.EventId == 0, 
//             new CustomConsoleLogger.CustomConsoleLoggerConfiguration
//             {
//                 EventId = 0,
//                 TimestampPrefix = "[HH:mm:ss]"
//             }));
// });