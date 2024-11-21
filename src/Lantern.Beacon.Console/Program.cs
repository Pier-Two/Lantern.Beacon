using Google.Protobuf;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types;
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
        var beaconClientOptions = new BeaconClientOptions
        {
            TcpPort = 9005,
            DialTimeoutSeconds = 10,
            MaxParallelDials = 10,
            EnableDiscovery = false,
            GossipSubEnabled = true,
            TargetPeerCount = 1,
            Bootnodes = ["/ip4/0.0.0.0/tcp/9018/p2p/16Uiu2HAm4FHaqCj6S8kPbijYT8t9smAozZbNCiyP14oFX6duYtD4"],
            //Bootnodes = ["/ip4/168.70.69.103/tcp/9002/p2p/16Uiu2HAmHnBu4zPM6mbssPUa5JfKfuqk2EZFFD4dtkzCvBLv1H98"],
            SyncProtocolOptions = new SyncProtocolOptions
            {
                Preset = SizePreset.MainnetPreset,
                GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
                GenesisTime = 1606824023,
                TrustedBlockRoot = Convert.FromHexString("c13f2ed79f62f36129b2acd723876598a81deca464b401352b8a73e9e91b710b"),
                Network = NetworkType.Mainnet
            }
        };
        var discoveryBootnodes = new[]
        {
            "enr:-Ku4QHqVeJ8PPICcWk1vSn_XcSkjOkNiTg6Fmii5j6vUQgvzMc9L1goFnLKgXqBJspJjIsB91LTOleFmyWWrFVATGngBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC1MD8qAAAAAP__________gmlkgnY0gmlwhAMRHkWJc2VjcDI1NmsxoQKLVXFOhp2uX6jeT0DvvDpPcU8FWMjQdR4wMuORMhpX24N1ZHCCIyg",
            "enr:-Ku4QG-2_Md3sZIAUebGYT6g0SMskIml77l6yR-M_JXc-UdNHCmHQeOiMLbylPejyJsdAPsTHJyjJB2sYGDLe0dn8uYBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC1MD8qAAAAAP__________gmlkgnY0gmlwhBLY-NyJc2VjcDI1NmsxoQORcM6e19T1T9gi7jxEZjk_sjVLGFscUNqAY9obgZaxbIN1ZHCCIyg",
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg", 
            "enr:-Ku4QEWzdnVtXc2Q0ZVigfCGggOVB2Vc1ZCPEc6j21NIFLODSJbvNaef1g4PxhPwl_3kax86YPheFUSLXPRs98vvYsoBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpC1MD8qAAAAAP__________gmlkgnY0gmlwhDZBrP2Jc2VjcDI1NmsxoQM6jr8Rb1ktLEsVcKAPa08wCsKUmvoQ8khiOl_SLozf9IN1ZHCCIyg",
            "enr:-Le4QPUXJS2BTORXxyx2Ia-9ae4YqA_JWX3ssj4E_J-3z1A-HmFGrU8BpvpqhNabayXeOZ2Nq_sbeDgtzMJpLLnXFgAChGV0aDKQtTA_KgEAAAAAIgEAAAAAAIJpZIJ2NIJpcISsaa0Zg2lwNpAkAIkHAAAAAPA8kv_-awoTiXNlY3AyNTZrMaEDHAD2JKYevx89W0CcFJFiskdcEzkH_Wdv9iW42qLK79ODdWRwgiMohHVkcDaCI4I"
        };
        var connectionOptions = new ConnectionOptions();
        var sessionKeys = new SessionKeys(beaconClientOptions.Identity.PrivateKey.Data.ToByteArray());
        var sessionOptions = new SessionOptions
        {
            SessionKeys = sessionKeys,
            Signer = new IdentitySignerV4(sessionKeys.PrivateKey),
            Verifier = new IdentityVerifierV4()
        };
        var tableOptions = new TableOptions(discoveryBootnodes);
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        var discv5LoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder
                .SetMinimumLevel(LogLevel.Information)
                .AddFilter("Lantern.Beacon.Networking.Libp2pProtocols.Identify.PeerIdentifyProtocol", LogLevel.None)
                .AddFilter("Nethermind.Libp2p.Core.ChannelFactory", LogLevel.None)
                .AddFilter("Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.GossipsubProtocolV12", LogLevel.None)
                .AddFilter("Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub.CustomPubsubRouter", LogLevel.None)
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
