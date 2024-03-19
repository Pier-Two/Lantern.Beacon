using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Beacon;

public class BeaconClientBuilder
{
    private IServiceCollection _services = new ServiceCollection();
    private string[] _bootstrapEnrs = Array.Empty<string>();

    public BeaconClientBuilder WithBootstrapEnrs(string[] bootstrapEnrs)
    {
        _bootstrapEnrs = bootstrapEnrs;
        return this;
    }

    public BeaconClient Build()
    {
        // Configure services including Lantern.Discv5
        BeaconServiceConfiguration.ConfigureServices(_services, _bootstrapEnrs);
        var serviceProvider = _services.BuildServiceProvider();
        return new BeaconClient(serviceProvider);
    }
}