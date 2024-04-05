using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Multiformats.Hash;

using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Stack;
using NUnit.Framework;

namespace Lantern.Beacon.SyncProtocol.Tests;

[TestFixture]
public class CoreTests
{
    [Test]
    public async Task Test1()
    {
       var bootstrapEnrs = new[]
        {
            "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg",
            "enr:-Le4QPUXJS2BTORXxyx2Ia-9ae4YqA_JWX3ssj4E_J-3z1A-HmFGrU8BpvpqhNabayXeOZ2Nq_sbeDgtzMJpLLnXFgAChGV0aDKQtTA_KgEAAAAAIgEAAAAAAIJpZIJ2NIJpcISsaa0Zg2lwNpAkAIkHAAAAAPA8kv_-awoTiXNlY3AyNTZrMaEDHAD2JKYevx89W0CcFJFiskdcEzkH_Wdv9iW42qLK79ODdWRwgiMohHVkcDaCI4I"
        };
        var connectionOptions = new ConnectionOptions
        {
            UdpPort = new Random().Next(1, 65535)
        };
        var sessionOptions = SessionOptions.Default;
        var tableOptions = new TableOptions(bootstrapEnrs);
        var enr = new EnrBuilder()
            .WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer)
            .WithEntry(EnrEntryKey.Id, new EntryId("v4"))
            .WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        var services = new ServiceCollection();
        // services.AddLogging(builder =>
        //     {
        //         builder.AddConsole().SetMinimumLevel(LogLevel.Debug)
        //                 .AddSimpleConsole(options =>
        //                 {
        //                     options.ColorBehavior = LoggerColorBehavior.Default;
        //                     options.IncludeScopes = false;
        //                     options.SingleLine = true;
        //                     options.TimestampFormat = "[HH:mm:ss] ";
        //                     options.UseUtcTimestamp = true;
        //                 });
        //     })
        //     .AddLibp2p(builder => builder)
        //     .AddBeaconClient(builder =>
        //     {
        //         builder.WithDiscv5ProtocolBuilder(discv5Builder =>
        //         {
        //             discv5Builder.WithConnectionOptions(connectionOptions)
        //                 .WithTableOptions(tableOptions)
        //                 .WithEnrBuilder(enr)
        //                 .WithSessionOptions(sessionOptions)
        //                 .Build();
        //         });
        //     });
        //     
        // var serviceProvider = services.BuildServiceProvider();
        // var peerFactory = serviceProvider.GetService<IPeerFactory>();
        // var localPeer = peerFactory!.Create(new Identity());
        //
        // localPeer.Address = localPeer.Address.Replace<IP4>("0.0.0.0").Replace<TCP>(0);
        //
        // var discoveryProtocol = serviceProvider.GetRequiredService<DiscoveryProtocol>();
        //
        // await discoveryProtocol.StartAsync();
        //
        // var multiAddresses = await discoveryProtocol.DiscoverAsync();
        //
        // foreach (var address in multiAddresses)
        // {
        //     Console.WriteLine("Dialing peer with multiaddress: " + address);
        //     var cts = new CancellationTokenSource();
        //     cts.CancelAfter(TimeSpan.FromSeconds(5)); 
        //
        //     try
        //     {
        //         var dialTask = localPeer.DialAsync(address, cts.Token);
        //         var completedTask = await Task.WhenAny(dialTask, Task.Delay(Timeout.Infinite, cts.Token));
        //         if (completedTask == dialTask)
        //         {
        //             // Dial task completed within timeout
        //             var remotePeer = await dialTask; // Ensure any exceptions are rethrown
        //             Console.WriteLine("Dial task completed successfully");
        //             Console.WriteLine("Peer responded " + address);
        //         }
        //         else
        //         {
        //             Console.WriteLine("Dial task timed out");
        //         }
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         Console.WriteLine("Dial task was canceled.");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"An error occurred: {ex.Message}");
        //     }
        // }
    }
}