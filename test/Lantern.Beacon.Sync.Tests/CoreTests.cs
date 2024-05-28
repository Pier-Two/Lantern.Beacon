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
using Nethermind.Libp2p.Stack;
using NUnit.Framework;

namespace Lantern.Beacon.Sync.Tests;

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
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Default;
                    options.SingleLine = true;
                    options.TimestampFormat = "[HH:mm:ss] ";
                    options.UseUtcTimestamp = true;
                });
            })
            .AddLibp2p(builder => builder);
        
        var serviceProvider = services.BuildServiceProvider();
        var peerFactory = serviceProvider.GetService<IPeerFactory>();
        var myIdentity = new Identity();
        
        ILocalPeer? localPeer = null;

        if (peerFactory is not null)
        {
            localPeer = peerFactory.Create(myIdentity);
        }

        var newAddress =  localPeer.Address.ReplaceOrAdd<IP4>("0.0.0.0").ReplaceOrAdd<TCP>(0);
        localPeer.Address = newAddress;
        
        var remoteAddress = Multiaddress.Decode("/ip4/49.12.174.22/tcp/9000/p2p/16Uiu2HAmTUmUjvL9mts7C3iKcv8vtmrwWyXp2AWBhLcjuwxGdTWk");
        
        Console.WriteLine("My peer's multiaddress is: " + localPeer.Address);
        Console.WriteLine("Dialing peer with multiaddress: " + remoteAddress);
        
        var task = localPeer.DialAsync(remoteAddress);

        await Task.WhenAny(task, Task.Delay(5000));
    }
}