using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols.Pubsub;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddBeaconClient(this IServiceCollection services, Action<IBeaconClientServiceBuilder> configureBeaconClientService)
    {
        var beaconClientServiceBuilder = new BeaconClientServiceBuilder(services);
        
        configureBeaconClientService(beaconClientServiceBuilder);

        services.AddSingleton(beaconClientServiceBuilder.Build());
        
        return services;
    }
}