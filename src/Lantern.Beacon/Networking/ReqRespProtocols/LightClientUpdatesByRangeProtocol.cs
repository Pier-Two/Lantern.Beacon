using Lantern.Beacon.Sync;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class LightClientUpdatesByRangeProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    public string Id => "/eth2/beacon_chain/req/light_client_updates_by_range/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        throw new NotImplementedException();
    }

    public Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        throw new NotImplementedException();
    }
}