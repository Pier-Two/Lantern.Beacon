using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public interface IBeaconClientServiceBuilder
{
    IBeaconClientServiceBuilder AddDiscoveryProtocol(Action<IDiscv5ProtocolBuilder> configure);

    IBeaconClientServiceBuilder AddLibp2pProtocol(
        Func<ILibp2pPeerFactoryBuilder, IPeerFactoryBuilder> factorySetup);
    
    IBeaconClientServiceBuilder WithBeaconClientOptions(Action<BeaconClientOptions> configure);
    
    IBeaconClientServiceBuilder WithBeaconClientOptions(BeaconClientOptions options);

    IBeaconClient Build();
}