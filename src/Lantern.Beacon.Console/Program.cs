using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon.Console;

internal static class Program
{
    public static async Task Main(string[] args)
    {
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
            }).AddLibp2p(builder => builder);
        
        var serviceProvider = services.BuildServiceProvider();
        var peerFactory = serviceProvider.GetService<IPeerFactory>();
        var localPeer = peerFactory!.Create(new Identity());
        localPeer.Address = localPeer.Address.Replace<IP4>("0.0.0.0").Replace<TCP>(0);
        var beaconClient = new BeaconClientBuilder()
            .WithBootstrapEnrs(["enr:-KG4QNTx85fjxABbSq_Rta9wy56nQ1fHK0PewJbGjLm1M4bMGx5-3Qq4ZX2-iFJ0pys_O90sVXNNOxp2E7afBsGsBrgDhGV0aDKQu6TalgMAAAD__________4JpZIJ2NIJpcIQEnfA2iXNlY3AyNTZrMaECGXWQ-rQ2KZKRH1aOW4IlPDBkY4XDphxg9pxKytFCkayDdGNwgiMog3VkcIIjKA", 
                "enr:-Ku4QImhMc1z8yCiNJ1TyUxdcfNucje3BGwEHzodEZUan8PherEo4sF7pPHPSIB1NNuSg5fZy7qFsjmUKs2ea1Whi0EBh2F0dG5ldHOIAAAAAAAAAACEZXRoMpD1pf1CAAAAAP__________gmlkgnY0gmlwhBLf22SJc2VjcDI1NmsxoQOVphkDqal4QzPMksc5wnpuC3gvSC8AfbFOnZY_On34wIN1ZHCCIyg"])
            .Build();
        
        await beaconClient.Init();
        var randomId = new byte[32];
        Random.Shared.NextBytes(randomId);
        
        var peers = await beaconClient.Discv5Protocol.PerformLookupAsync(randomId);
        var mutliAddressStrings = peers.Select(peer => ConvertToTCPMultiaddress(peer.Record)).ToList();
        var remoteAddresses = mutliAddressStrings.Select(Multiaddress.Decode).ToList();

        foreach (var address in mutliAddressStrings)
        {
          System.Console.WriteLine(address);  
        }

        foreach (var address in remoteAddresses)
        {
            System.Console.WriteLine("Dialing peer with multiaddress: " + address);
            var dialTask = localPeer.DialAsync(address);
            var remotePeer = await dialTask;
            
            if(dialTask.IsCompletedSuccessfully)
            {
                System.Console.WriteLine("Dial task completed successfully");
                System.Console.WriteLine("Peer responded " + address);
            }
            else
            {
                System.Console.WriteLine("Dial task failed");
            }
        }
        
        //var dialTask = localPeer.DialAsync(remoteAddress);
        //var remotePeer = await dialTask;
    }
    
    private static string ConvertToTCPMultiaddress(IEnr enr)
    {
        var ip = enr.GetEntry<EntryIp>(EnrEntryKey.Ip)?.Value;
        var tcp = enr.GetEntry<EntryTcp>(EnrEntryKey.Tcp)?.Value;
        return $"/ip4/{ip}/tcp/{tcp}/p2p/{enr.ToPeerId()}";
    }
}