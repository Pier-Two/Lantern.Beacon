using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon.Console;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // Discover peers and connect to them
        var beaconClient = new BeaconClientBuilder()
            .WithBootstrapEnrs(["enr:-KG4QNTx85fjxABbSq_Rta9wy56nQ1fHK0PewJbGjLm1M4bMGx5-3Qq4ZX2-iFJ0pys_O90sVXNNOxp2E7afBsGsBrgDhGV0aDKQu6TalgMAAAD__________4JpZIJ2NIJpcIQEnfA2iXNlY3AyNTZrMaECGXWQ-rQ2KZKRH1aOW4IlPDBkY4XDphxg9pxKytFCkayDdGNwgiMog3VkcIIjKA", 
                "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg"])
            .Build();
        
        await beaconClient.Init();
        var randomId = new byte[32];
        Random.Shared.NextBytes(randomId);
        
        var peers = await beaconClient.Discv5Protocol.PerformLookupAsync(randomId);
        
        var services = new ServiceCollection()
            .AddLogging(builder => 
            {
                builder
                    .AddConsole().SetMinimumLevel(LogLevel.Debug)
                    .AddSimpleConsole(options =>
                    {
                        options.ColorBehavior = LoggerColorBehavior.Enabled;
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                        options.TimestampFormat = "[HH:mm:ss] ";
                        options.UseUtcTimestamp = true;
                    });
            })
            .AddSingleton(new IdentifyProtocolSettings
            {
                
            })
            .AddLibp2p(builder =>
            {
                builder.AddAppLayerProtocol<PingProtocol>();
                
                return builder;
            });
        
        var serviceProvider = services.BuildServiceProvider();
        var peerFactory = serviceProvider.GetService<IPeerFactory>();
        var localPeer = peerFactory!.Create(new Identity());
        var newAddress = localPeer.Address.ReplaceOrAdd<IP4>("0.0.0.0").ReplaceOrAdd<TCP>(0);
        
        localPeer.Address = newAddress;
        
        var remoteAddress = Multiaddress.Decode("/ip4/138.201.127.100/tcp/9000/p2p/16Uiu2HAm7GNUzxYr53ixJvkeVBvz185Vms4Nx6ydVnTdCX7jtN5F");
        
        System.Console.WriteLine("My peer's multiaddress is: " + localPeer.Address);
        System.Console.WriteLine("Dialing peer with multiaddress: " + remoteAddress);
        
        var dialTask = localPeer.DialAsync(remoteAddress);
        var remotePeer = await dialTask;
        
        System.Console.WriteLine(remotePeer.Address);
    }
}