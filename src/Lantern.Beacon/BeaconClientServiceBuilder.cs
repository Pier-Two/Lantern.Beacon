using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;
using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Lantern.Beacon.Sync;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;

namespace Lantern.Beacon;

public class BeaconClientServiceBuilder(IServiceCollection services) : IBeaconClientServiceBuilder
{
    private SyncProtocolOptions _syncProtocolOptions = new();
    private BeaconClientOptions _beaconClientOptions = new();
    private IDiscv5ProtocolBuilder _discv5ProtocolBuilder = new Discv5ProtocolBuilder(services);
    private IServiceProvider? _serviceProvider;
    private ILoggerFactory _loggerFactory = LoggingOptions.Default;
    
    public IBeaconClientServiceBuilder AddDiscoveryProtocol(Action<IDiscv5ProtocolBuilder> configure)
    {
        configure(_discv5ProtocolBuilder ?? throw new ArgumentNullException(nameof(configure)));
        return this;
    }

    public IBeaconClientServiceBuilder AddLibp2pProtocol(Func<ILibp2pPeerFactoryBuilder, IPeerFactoryBuilder> factorySetup)
    {
        services.AddScoped(sp => factorySetup(new BeaconClientPeerFactoryBuilder(sp)))
            .AddScoped<CustomPubsubRouter>()
            .AddScoped<MultiplexerSettings>()
            .AddScoped<IdentifyProtocolSettings>(_ => new IdentifyProtocolSettings
            {
                AgentVersion = LanternIdentitySettings.AgentVersion,
                ProtocolVersion = LanternIdentitySettings.ProtocolVersion
            })
            .AddScoped(sp => sp.GetService<IPeerFactoryBuilder>()!.Build());
        
        return this;
    }
    
    public IBeaconClientServiceBuilder WithSyncProtocolOptions(Action<SyncProtocolOptions> configure)
    {
        configure(_syncProtocolOptions);
        return this;
    }
    
    public IBeaconClientServiceBuilder WithSyncProtocolOptions(SyncProtocolOptions options)
    {
        _syncProtocolOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }
    
    public IBeaconClientServiceBuilder WithBeaconClientOptions(Action<BeaconClientOptions> configure)
    {
        configure(_beaconClientOptions);
        return this;
    }
    
    public IBeaconClientServiceBuilder WithBeaconClientOptions(BeaconClientOptions options)
    {
        _beaconClientOptions = options ?? throw new ArgumentNullException(nameof(options));
        return this;
    }
    
    public IBeaconClientServiceBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        return this;
    }
    
    public IBeaconClient Build()
    {
        services.AddBeaconClient(_discv5ProtocolBuilder.Build(), _beaconClientOptions, _syncProtocolOptions, _loggerFactory);
        _serviceProvider = services.BuildServiceProvider();
        
        return _serviceProvider.GetRequiredService<IBeaconClient>();
    }
}