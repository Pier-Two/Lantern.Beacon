using Lantern.Beacon.Networking;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Enums;
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
        var connectionOptions = new ConnectionOptions();
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

                beaconClientBuilder.WithBeaconClientOptions(options => options.TcpPort = 30303);
                beaconClientBuilder.AddLibp2pProtocol(libp2PBuilder => libp2PBuilder);
                
            });
        
        var serviceProvider = services.BuildServiceProvider();
        var peerFactory = serviceProvider.GetService<IPeerFactory>();
        var myIdentity = new Identity();
        
        ILocalPeer? localPeer = null;

        if (peerFactory is not null)
        {
            localPeer = peerFactory.Create(myIdentity);
        }

        var newAddress =  localPeer.Address.Replace<IP4>("0.0.0.0").Replace<TCP>(0);
        localPeer.Address = newAddress;
        
        var remoteAddress = Multiaddress.Decode("/ip4/167.235.117.215/tcp/9000/p2p/16Uiu2HAmHcm7WxUx9Pi6wv8b391XNw4qbsccUzocCermGSQTc1WC");
        
        Console.WriteLine("My peer's multiaddress is: " + localPeer.Address);
        Console.WriteLine("Dialing peer with multiaddress: " + remoteAddress);
        
        var task = localPeer.DialAsync(remoteAddress);

        await Task.WhenAny(task, Task.Delay(5000));
    }
}