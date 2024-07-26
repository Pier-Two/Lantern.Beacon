using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SszSharp;

namespace Lantern.Beacon.Console;

public class NodeTableEntry {
    public Guid Id { get; set; }
}

internal static class Program
{ 
    public static async Task Main()
    {
        var bootstrapEnrs = new[]
        {
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg",
            "enr:-Le4QPUXJS2BTORXxyx2Ia-9ae4YqA_JWX3ssj4E_J-3z1A-HmFGrU8BpvpqhNabayXeOZ2Nq_sbeDgtzMJpLLnXFgAChGV0aDKQtTA_KgEAAAAAIgEAAAAAAIJpZIJ2NIJpcISsaa0Zg2lwNpAkAIkHAAAAAPA8kv_-awoTiXNlY3AyNTZrMaEDHAD2JKYevx89W0CcFJFiskdcEzkH_Wdv9iW42qLK79ODdWRwgiMohHVkcDaCI4I"
        };
        var connectionOptions = new ConnectionOptions
        {
            UdpPort = 4555
        };
        //var sessionKeys = new SessionKeys(Convert.FromHexString("F57EC7A295ED7F7FE54DD155C36F64384FC78D7D48C20FB7D415DE4E99575EA3"));
        // var sessionOptions = new SessionOptions
        // {
        //     SessionKeys = sessionKeys,
        //     Signer = new IdentitySignerV4(sessionKeys.PrivateKey),
        //     Verifier = new IdentityVerifierV4(),
        //     SessionCacheSize = 1000
        // };
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions(bootstrapEnrs)
        {
            MaxNodesCount = 16
        };
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        var services = new ServiceCollection();
        var discv5LoggerFactory = LoggerFactory.Create(builder =>
            builder.SetMinimumLevel(LogLevel.None)
                .AddSimpleConsole(l => {
                l.SingleLine = true; 
                l.TimestampFormat = "[HH:mm:ss] ";
                l.ColorBehavior = LoggerColorBehavior.Default;
                l.IncludeScopes = false;
                l.UseUtcTimestamp = true;
            }));
        var libp2p2LoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                // .AddFilter((category, level) =>
                // {
                //     if (category.StartsWith("Nethermind.Libp2p.Protocols.Pubsub.GossipsubProtocolV11") || category.StartsWith("Nethermind.Libp2p.Protocols.Pubsub.GossipsubProtocol"))
                //     {
                //         return false;
                //     }
                //     return level >= LogLevel.Information;
                // })
                .AddSimpleConsole(l =>
                {
                    l.SingleLine = true;
                    l.TimestampFormat = "[HH:mm:ss] ";
                    l.ColorBehavior = LoggerColorBehavior.Default;
                    l.IncludeScopes = false;
                    l.UseUtcTimestamp = true;
                });
        });
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
                beaconClientBuilder.WithBeaconClientOptions(options =>
                {
                    options.TcpPort = 9005;
                    options.EnableDiscovery = true;
                    //options.Bootnodes = ["/ip4/50.195.130.74/tcp/9000/p2p/16Uiu2HAkvSLFzPogiUZn1wFEskrUoJt9DGot3PbfeSE5zHqS32FM"];
                    //options.Bootnodes = ["/ip4/73.186.232.187/tcp/9105/p2p/16Uiu2HAm37UA7fk8r2AnYtGLbddwkS2WEeSPTsjNDGh3gDW7VUBQ"]; // Teku
                    //options.Bootnodes = ["/ip4/69.175.102.62/tcp/31018/p2p/16Uiu2HAm2FWXMoKEsshxjXNsXmFwxPAm4eaWmcffFTGgNs3gi4Ww"]; // Erigon
                    //options.Bootnodes = ["/ip4/0.0.0.0/tcp/9000/p2p/16Uiu2HAm6R996q426GYUyExKSYdKxhbD5iYedbuqQovVPTJFVHPv"];
                    //options.Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAkwvVXtZj6u3R2F7hEXpnbDUom3rDepABdDCSzyzAM2k69"];
                    //options.Bootnodes = ["/ip4/54.38.80.34/tcp/9000/p2p/16Uiu2HAm8t1aQArVwrJ9fwHRGXL2sXumPGTvmsne14piPaFJ5FYi"]; // Lighthouse
                    //options.Bootnodes = ["/ip4/37.27.63.66/tcp/9115/p2p/16Uiu2HAm8BCbnKxJnsNq6uJAhGe3wNrUiiLCTete2vP5UUT99oNL"];
                    options.Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAkwvVXtZj6u3R2F7hEXpnbDUom3rDepABdDCSzyzAM2k69"]; // Lodestar
                    //options.Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAmQW7R658hXDAGvR9mRr56JyX4UJpcB5KiGoDv4ENyBFX1"];
                    //options.Bootnodes = ["/ip4/0.0.0.0/tcp/9000/p2p/16Uiu2HAm6R996q426GYUyExKSYdKxhbD5iYedbuqQovVPTJFVHPv"];
                });
                beaconClientBuilder.WithSyncProtocolOptions(syncProtocol =>
                {
                    syncProtocol.Preset = SizePreset.MainnetPreset;
                    syncProtocol.GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95");
                    syncProtocol.GenesisTime = 1606824023;
                    syncProtocol.TrustedBlockRoot = Convert.FromHexString("b6ae5813712eb99c84c3e53a7e91b5cf5e82722297e03432be3de6e4da05f1a4");
                });
                beaconClientBuilder.AddLibp2pProtocol(libp2PBuilder => libp2PBuilder);
                beaconClientBuilder.WithLoggerFactory(libp2p2LoggerFactory);
            });
        
        var serviceProvider = services.BuildServiceProvider();
        var beaconClient = serviceProvider.GetRequiredService<IBeaconClient>();

        await beaconClient.InitAsync();
        await beaconClient.StartAsync();
    }
}