using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Sync;
using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Beacon;

public static class BeaconClientServiceConfiguration
{
    internal static IServiceCollection AddBeaconClient(this IServiceCollection services, IDiscv5Protocol discv5, BeaconClientOptions beaconClientOptions, SyncProtocolOptions syncProtocolOptions)
    {
        services.AddSingleton(discv5);
        services.AddSingleton(beaconClientOptions);
        services.AddSingleton(syncProtocolOptions);
        services.AddSingleton<IDiscoveryProtocol, DiscoveryProtocol>();
        services.AddSingleton<IPeerManager, PeerManager>();
        services.AddSingleton<ISyncProtocol, SyncProtocol>();
        services.AddSingleton<IBeaconClient, BeaconClient>();
        
        return services;
    }

}

