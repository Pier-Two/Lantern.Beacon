using Lantern.Discv5.WireProtocol;
using Microsoft.Extensions.DependencyInjection;

namespace Lantern.Beacon;

public class BeaconClient(IServiceProvider serviceProvider)
{
    public readonly Discv5Protocol Discv5Protocol = serviceProvider.GetRequiredService<Discv5Protocol>();

    public async Task Init()
    {
        await Discv5Protocol.StartProtocolAsync();
    }
}