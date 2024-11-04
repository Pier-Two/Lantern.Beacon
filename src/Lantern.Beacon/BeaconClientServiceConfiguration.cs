using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Networking.RestApi;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core.Discovery;

namespace Lantern.Beacon;

public static class BeaconClientServiceConfiguration
{
    internal static IServiceCollection AddBeaconClient(this IServiceCollection services, IDiscv5Protocol discv5, BeaconClientOptions beaconClientOptions, ILoggerFactory loggerFactory)
    {
        services.AddSingleton(discv5);
        services.AddSingleton(beaconClientOptions);
        services.AddSingleton(beaconClientOptions.SyncProtocolOptions);
        services.AddSingleton(loggerFactory);
        services.AddSingleton<ManualDiscoveryProtocol>();
        services.AddSingleton<ICustomDiscoveryProtocol, CustomDiscoveryProtocol>();
        services.AddSingleton<IBeaconClientManager, BeaconClientManager>();
        services.AddSingleton<IHttpServer, HttpServer>();
        services.AddSingleton<ISyncProtocol, SyncProtocol>();
        services.AddSingleton<ILiteDbService, LiteDbService>();
        services.AddSingleton<IPeerState, PeerState>();
        services.AddSingleton<IBeaconClient, BeaconClient>();
        services.AddSingleton<IGossipSubManager, GossipSubManager>();
        
        return services;
    }
}

