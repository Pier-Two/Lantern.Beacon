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
            TrustedBlockRoot = Convert.FromHexString("50344e0f03151e3edce334698b59eaf7ba40270984e1c0cde9393879564b40b6"),
            Network = NetworkType.Mainnet
        };
        var beaconClientOptions = new BeaconClientOptions
        {
            TcpPort = 9005,
            DialTimeoutSeconds = 5,
            MaxParallelDials = 5,
            //Bootnodes = ["/ip4/194.33.40.72/tcp/9001/p2p/16Uiu2HAmKnikHhMXdEYzDqXDAUuqnjQNwmTZuWNVyo73aHePjgvq"] // Public Nimbus Full Client
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAmH5mNHiU3nepLqhUQ5gZvYK7EcJi5BYBV4XzUBuTcxGw4"] // Local Nimbus Full Client
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9014/p2p/16Uiu2HAm8fBXikjEJnzSvjwuSRozFacyLQQWg8ajHxqN3HGbs2zT"] // Local Lodestar Full Client
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAmSift7QRDZg1TP9Epw5Ae5nz66Nv7hQry4bx4zFkMFr2W"] // Local Nimbus Light Client
            //Bootnodes = ["/ip4/73.186.232.187/tcp/9105/p2p/16Uiu2HAm37UA7fk8r2AnYtGLbddwkS2WEeSPTsjNDGh3gDW7VUBQ"], // Teku
            //Bootnodes = ["/ip4/69.175.102.62/tcp/31018/p2p/16Uiu2HAm2FWXMoKEsshxjXNsXmFwxPAm4eaWmcffFTGgNs3gi4Ww"], // Erigon
            //Bootnodes = ["/ip4/136.243.72.174/tcp/9000/p2p/16Uiu2HAkvTExpESzdQfYg6N3RtX6YNerBXTeg6vXzTMAws3tUQCA"],
            //Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAkwvVXtZj6u3R2F7hEXpnbDUom3rDepABdDCSzyzAM2k69"],
            //Bootnodes = ["/ip4/54.38.80.34/tcp/9000/p2p/16Uiu2HAm8t1aQArVwrJ9fwHRGXL2sXumPGTvmsne14piPaFJ5FYi"], // Lighthouse
            //Bootnodes = ["/ip4/37.27.63.66/tcp/9115/p2p/16Uiu2HAm8BCbnKxJnsNq6uJAhGe3wNrUiiLCTete2vP5UUT99oNL"],
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
        services.AddBeaconClient(beaconClientBuilder =>
            {
                beaconClientBuilder.AddDiscoveryProtocol(discv5Builder =>
                {
                    discv5Builder.WithConnectionOptions(connectionOptions)
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

        await beaconClient.InitAsync();
        await beaconClient.StartAsync();
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