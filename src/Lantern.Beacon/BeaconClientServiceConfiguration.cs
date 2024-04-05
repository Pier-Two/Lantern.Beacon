using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public static class BeaconClientServiceConfiguration
{
    internal static IServiceCollection AddBeaconClient(this IServiceCollection services, IDiscv5Protocol discv5)
    {
        Console.WriteLine("Configuring BeaconClientService...");
        
        services.AddSingleton(discv5);
        services.AddSingleton<IDiscoveryProtocol, DiscoveryProtocol>();
        services.AddSingleton<IPeerManager, PeerManager>();
        services.AddSingleton<IBeaconClient, BeaconClient>();

        return services;
    }

}

