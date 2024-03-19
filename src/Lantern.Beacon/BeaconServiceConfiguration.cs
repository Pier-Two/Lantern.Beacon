using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Beacon;

public static class BeaconServiceConfiguration
{
    public static IServiceCollection ConfigureServices(IServiceCollection services, string[] bootstrapEnrs)
    {
        var discv5 = Discv5Builder.CreateDefault(bootstrapEnrs);
        services.AddSingleton(discv5); // Register Discv5Protocol as a singleton service

        return services;
    }
}