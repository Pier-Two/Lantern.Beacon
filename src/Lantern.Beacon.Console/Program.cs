using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Utility;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols.Pubsub;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon.Console;

internal static class Program
{
    public static async Task Main(string[] args)
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
        
        services.AddLogging(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Default;
                    options.IncludeScopes = false;
                    options.SingleLine = true;
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.UseUtcTimestamp = true;
                });
            })
            .AddBeaconClient(beaconClientBuilder =>
            {
                beaconClientBuilder.AddDiscoveryProtocol(discv5Builder =>
                {
                    discv5Builder.WithConnectionOptions(connectionOptions)
                        .WithTableOptions(tableOptions)
                        .WithEnrBuilder(enr)
                        .WithSessionOptions(sessionOptions);
                });

                beaconClientBuilder.AddLibp2pProtocol(libp2PBuilder => libp2PBuilder);
            });
        
        var serviceProvider = services.BuildServiceProvider();
        var beaconClient = serviceProvider.GetRequiredService<IBeaconClient>();
        
        await beaconClient.InitAsync();
        await beaconClient.StartAsync();
        // await discoveryProtocol!.StartAsync();
        // await discoveryProtocol.DiscoverAsync();
        //
        // var selfEnr = discoveryProtocol.SelfEnr;
        // var ip = selfEnr.GetEntry<EntryIp>(EnrEntryKey.Ip).Value;
        // var tcpPort = selfEnr.GetEntry<EntryUdp>(EnrEntryKey.Udp).Value + 1;
        // localPeer.Address = localPeer.Address.Replace<IP4>(ip).Replace<TCP>(tcpPort);
        // var multiAddresses = discoveryProtocol.ActiveNodes.Select(MultiAddressEnrConverter.ConvertToMultiAddress);
        //
        // foreach (var address in multiAddresses)
        // {
        //     if(address == null)
        //     {
        //         continue;
        //     }
        //     
        //     System.Console.WriteLine("Dialing peer with MultiAddress: " + address);
        //     var cts = new CancellationTokenSource();
        //     cts.CancelAfter(TimeSpan.FromSeconds(2)); 
        //
        //     try
        //     {
        //         var dialTask = localPeer.DialAsync(address, cts.Token);
        //         await Task.WhenAny(dialTask, Task.Delay(Timeout.Infinite, cts.Token));
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         System.Console.WriteLine("Dial task was canceled.");
        //     }
        //     catch (Exception ex)
        //     {
        //         System.Console.WriteLine($"An error occurred: {ex.Message}");
        //     }
        // }
    }
}