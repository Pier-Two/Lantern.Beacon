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
    // private static List<NodeTableEntry> bucket = new List<NodeTableEntry>();
    // private static Dictionary<Guid, NodeTableEntry> routingTable = new Dictionary<Guid, NodeTableEntry>();
    // private static List<HashSet<Guid>> _pathBuckets = new List<HashSet<Guid>>();
    // private static HashSet<Guid> requestManager = new HashSet<Guid>();
    //
    // static void Main() {
    //     // Setup a large number of nodes for the test
    //     for (int i = 0; i < 10000; i++) {
    //         var id = Guid.NewGuid();
    //         bucket.Add(new NodeTableEntry { Id = id });
    //         if (i % 2 == 0) routingTable[id] = new NodeTableEntry { Id = id };
    //         if (i % 3 == 0) requestManager.Add(id);
    //         if (i % 4 == 0) _pathBuckets.Add(new HashSet<Guid> { id });
    //     }
    //     
    //     var senderNodeId = 0;  // Simulate the senderNodeId
    //     var queryCount = 1000;
    //     
    //     var sw = Stopwatch.StartNew();
    //     
    //     // LINQ approach
    //     sw.Restart();
    //     var nodesToQueryLinq = bucket
    //         .Where(node => routingTable.ContainsKey(node.Id))
    //         .Where(node => !_pathBuckets.Any(pathBucket => pathBucket.Contains(node.Id)))
    //         .Where(node => !requestManager.Contains(node.Id))
    //         .Take(queryCount)
    //         .ToList();
    //     sw.Stop();
    //     var linqTime = sw.ElapsedMilliseconds;
    //     System.Console.WriteLine($"LINQ approach took: {linqTime} ms");
    //     
    //     // Manual loop approach
    //     sw.Restart();
    //     var nodesToQueryLoop = new List<NodeTableEntry>();
    //     foreach (var node in bucket) {
    //         if (nodesToQueryLoop.Count >= queryCount)
    //             break;
    //         
    //         if (!routingTable.ContainsKey(node.Id))
    //             continue;
    //
    //         if (_pathBuckets.Any(pathBucket => pathBucket.Contains(node.Id)))
    //             continue;
    //
    //         if (requestManager.Contains(node.Id))
    //             continue;
    //
    //         nodesToQueryLoop.Add(node);
    //     }
    //     sw.Stop();
    //     var loopTime = sw.ElapsedMilliseconds;
    //     System.Console.WriteLine($"Manual loop approach took: {loopTime} ms");
    // }
    
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
        var sessionKeys = new SessionKeys(Convert.FromHexString("F57EC7A295ED7F7FE54DD155C36F64384FC78D7D48C20FB7D415DE4E99575EA3"));
        var sessionOptions = new SessionOptions
        {
            SessionKeys = sessionKeys,
            Signer = new IdentitySignerV4(sessionKeys.PrivateKey),
            Verifier = new IdentityVerifierV4(),
            SessionCacheSize = 1000
        };
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
                .SetMinimumLevel(LogLevel.Information)
                //.AddFilter((category, level) => level != LogLevel.Error && level >= LogLevel.Information)
                .AddSimpleConsole(l =>
                {
                    l.SingleLine = true;
                    l.TimestampFormat = "[HH:mm:ss] ";
                    l.ColorBehavior = LoggerColorBehavior.Default;
                    l.IncludeScopes = false;
                    l.UseUtcTimestamp = true;
                });
        });
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
                    options.TcpPort = 9000;
                    options.Bootnodes = ["/ip4/135.148.103.80/tcp/9000/p2p/16Uiu2HAmPYkvHs9HNDgRFnTAZyPT9qdXYumArdCQm922XrwsSSzJ"];
                });
                beaconClientBuilder.WithSyncProtocolOptions(syncProtocol =>
                {
                    syncProtocol.Preset = SizePreset.MainnetPreset;
                    syncProtocol.GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95");
                    syncProtocol.GenesisTime = 1606824023;
                    syncProtocol.TrustedBlockRoot = Convert.FromHexString("0e980533edbcba7b2ed6270c754d90e87b1a012170db7c67b6de43c1c9b94b7d");
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