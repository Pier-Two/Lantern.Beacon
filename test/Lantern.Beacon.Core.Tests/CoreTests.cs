using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;
using Nethermind.Libp2p.Stack;
using Console = System.Console;
using Multiaddress = Nethermind.Libp2p.Core.Multiaddr;
using Multiaddr = Nethermind.Libp2p.Core.Enums.Multiaddr;
using NUnit.Framework;

namespace Lantern.Beacon.Core.Tests;

[TestFixture]
public class CoreTests
{
    [Test]
    public async Task Test1()
    {
        var myIdentity = new Identity();
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .AddLibp2p(builder =>
            {
                builder.AddAppLayerProtocol<PingProtocol>();
                return builder;
            });

        var serviceProvider = services.BuildServiceProvider();
        var peerFactory = serviceProvider.GetService<IPeerFactory>();
        var peerFactoryBuilder = serviceProvider.GetService<IPeerFactoryBuilder>();

        foreach (var protocol in peerFactoryBuilder.AppLayerProtocols)
        {
            Console.WriteLine($"Protocol: {protocol}");
        }
        
        ILocalPeer? localPeer = null;

        if (peerFactory is not null)
        {
            localPeer = peerFactory.Create(myIdentity);
        }

        var newAddress = localPeer.Address.Replace(Multiaddr.Ip4, "0.0.0.0").Replace(Multiaddr.Tcp, "0");
        localPeer.Address = newAddress;

        var remoteAddress = new Multiaddress("/ip4/138.201.127.100/tcp/9000/p2p/16Uiu2HAm7GNUzxYr53ixJvkeVBvz185Vms4Nx6ydVnTdCX7jtN5F");
        
        Console.WriteLine("My peer's multiaddress is: " + localPeer.Address);
        Console.WriteLine("Dialing peer with multiaddress: " + remoteAddress);
        
        var task = localPeer.DialAsync(remoteAddress);
        // await task until 5 seconds or until task completes
        await Task.WhenAny(task, Task.Delay(5000));
    }
    
}