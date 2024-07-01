using System.Buffers;
using System.IO.Compression;
using System.Text;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync.Types.Deneb;
using Lantern.Beacon.Sync.Types.Phase0;
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
using SszSharp;
using Snappier;
using Snappy = IronSnappy.Snappy;

namespace Lantern.Beacon.Sync.Tests;

[TestFixture]
public class CoreTests
{
    [Test]
    public async Task Test1()
    {
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
        
        var remoteAddress = Multiaddress.Decode("/ip4/136.243.78.49/tcp/9000/p2p/16Uiu2HAmSCiF7XAW8TR9c27HoUnrynxPcMpp81w1bZZU7wpy1WY9");
        
        Console.WriteLine("My peer's multiaddress is: " + localPeer.Address);
        Console.WriteLine("Dialing peer with multiaddress: " + remoteAddress);
        
        var task = localPeer.DialAsync(remoteAddress);

        await Task.WhenAny(task, Task.Delay(5000));
    }
}