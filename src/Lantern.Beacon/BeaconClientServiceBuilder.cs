using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols.Pubsub;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon;

public class BeaconClientServiceBuilder(IServiceCollection services) : IBeaconClientServiceBuilder
{
    private IDiscv5ProtocolBuilder? _discv5ProtocolBuilder = new Discv5ProtocolBuilder(services);
    private IServiceProvider? _serviceProvider;
    
    public IBeaconClientServiceBuilder AddDiscoveryProtocol(Action<IDiscv5ProtocolBuilder> configure)
    {
        configure(_discv5ProtocolBuilder ?? throw new ArgumentNullException(nameof(configure)));
        return this;
    }

    public IBeaconClientServiceBuilder AddLibp2pProtocol(
        Func<ILibp2pPeerFactoryBuilder, IPeerFactoryBuilder> factorySetup)
    {
        services.AddScoped(sp => factorySetup(new Libp2pPeerFactoryBuilder(sp)))
            .AddScoped(sp => (ILibp2pPeerFactoryBuilder)factorySetup(new Libp2pPeerFactoryBuilder(sp)))
            .AddScoped(sp => sp.GetService<IPeerFactoryBuilder>()!.Build())
            .AddScoped<PubsubRouter>();
        
        return this;
    }
    
    public IServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? throw new InvalidOperationException("Build() must be called before accessing the service provider.");
    }
    
    public IBeaconClient Build()
    {
        if(_discv5ProtocolBuilder == null)
        {
            throw new ArgumentNullException(nameof(_discv5ProtocolBuilder));
        }
        
        services.AddBeaconClient(_discv5ProtocolBuilder.Build());
        _serviceProvider = services.BuildServiceProvider();
        
        return _serviceProvider.GetRequiredService<IBeaconClient>();
    }
}