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
        var discoveryBootnodes = new[]
        {
            "enr:-Ku4QHqVeJ8PPICcWk1vSn_XcSkjOkNiTg6Fmii5j6vUQgvzMc9L1goFnLKgXqBJspJjIsB91LTOleFmyWWrFVATGngBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC1MD8qAAAAAP__________gmlkgnY0gmlwhAMRHkWJc2VjcDI1NmsxoQKLVXFOhp2uX6jeT0DvvDpPcU8FWMjQdR4wMuORMhpX24N1ZHCCIyg",
            "enr:-Ku4QG-2_Md3sZIAUebGYT6g0SMskIml77l6yR-M_JXc-UdNHCmHQeOiMLbylPejyJsdAPsTHJyjJB2sYGDLe0dn8uYBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC1MD8qAAAAAP__________gmlkgnY0gmlwhBLY-NyJc2VjcDI1NmsxoQORcM6e19T1T9gi7jxEZjk_sjVLGFscUNqAY9obgZaxbIN1ZHCCIyg",
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg", 
            "enr:-Ku4QEWzdnVtXc2Q0ZVigfCGggOVB2Vc1ZCPEc6j21NIFLODSJbvNaef1g4PxhPwl_3kax86YPheFUSLXPRs98vvYsoBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC1MD8qAAAAAP__________gmlkgnY0gmlwhDZBrP2Jc2VjcDI1NmsxoQM6jr8Rb1ktLEsVcKAPa08wCsKUmvoQ8khiOl_SLozf9IN1ZHCCIyg",
            "enr:-Le4QPUXJS2BTORXxyx2Ia-9ae4YqA_JWX3ssj4E_J-3z1A-HmFGrU8BpvpqhNabayXeOZ2Nq_sbeDgtzMJpLLnXFgAChGV0aDKQtTA_KgEAAAAAIgEAAAAAAIJpZIJ2NIJpcISsaa0Zg2lwNpAkAIkHAAAAAPA8kv_-awoTiXNlY3AyNTZrMaEDHAD2JKYevx89W0CcFJFiskdcEzkH_Wdv9iW42qLK79ODdWRwgiMohHVkcDaCI4I"
        };
        var connectionOptions = new ConnectionOptions();
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions(discoveryBootnodes);
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        var discv5LoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));
        // Beacon client options
        var beaconClientOptions = new BeaconClientOptions
        {
            TcpPort = 9005,
            DialTimeoutSeconds = 10,
            MaxParallelDials = 10,
            EnableDiscovery = true,
            GossipSubEnabled = true,
            TargetPeerCount = 3,
            Bootnodes = ["/ip4/107.6.91.40/tcp/25532/p2p/16Uiu2HAmGt6EUXjRXGg4gfcSAxLD3ETpC1YA3HnqNLHLZPGEAC57"],
            SyncProtocolOptions = new SyncProtocolOptions
            {
                Preset = SizePreset.MainnetPreset,
                GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
                GenesisTime = 1606824023,
                TrustedBlockRoot = Convert.FromHexString("3cdedee5fddbe2c0bcf912a9078f2ab85b5c10d39800cf02bd5e1589db46863b"),
                Network = NetworkType.Mainnet
            }

            //Bootnodes = ["/ip4/162.19.222.38/tcp/15401/p2p/16Uiu2HAmLA7eWnZUnjFQNR7sa8uZumNGA5hPvW6wiWoW1cT2Xkgg"] // Good peer
            //Bootnodes = ["/ip4/194.33.40.78/tcp/9001/p2p/16Uiu2HAky7NHnmvJcE2Kq459qNrgczXvfqjdFtsAJ6HyAoZpP4zw"]
            //Bootnodes = ["/ip4/145.239.161.11/tcp/15401/p2p/16Uiu2HAkvcYHu3rHkJqs7VQyyHsdFq5fFyjKNR9VJnLDrFs93sb6"]
            //Bootnodes = ["/ip4/135.148.27.79/tcp/54873/p2p/16Uiu2HAmT4ah5oDAriP7sbjb834QEUm7NQdg4t4mTDfiyddF5vjo"]
            //Bootnodes = ["/ip4/94.16.205.215/tcp/9000/p2p/16Uiu2HAm9UL4C273yG9iBQS6RWavcxZDi5rGfSZXLvgd5vnDszCN"]
            //Bootnodes = ["/ip4/194.33.40.70/tcp/9002/p2p/16Uiu2HAmFytwaDJqRrXM4rNc7AKWeJjxWSjdtYRWTsfwZ7FzRA3m"]
            //Bootnodes = ["/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAm66XKpWs6y6pkGJAiDdxmrdhZG3JYrmmma32U5mNFcStY"]
            //Bootnodes = ["/ip4/62.195.159.165/tcp/9105/p2p/16Uiu2HAmCCqYTbo4nyemcFvNmuTTHYUDCUbmMShW5UsxAbf3TiUa"] Teku
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
                //.AddFilter("Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.GossipsubProtocolV11", LogLevel.Debug)
                //.AddFilter("Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.CustomPubsubRouter", LogLevel.Debug)
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
