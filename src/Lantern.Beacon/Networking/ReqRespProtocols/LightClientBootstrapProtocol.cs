using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqResp;

public class LightClientBootstrapProtocol : IProtocol
{
    public string Id => "/eth2/beacon_chain/req/light_client_bootstrap/1/";
    
    public Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        throw new NotImplementedException();
    }

    public Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        throw new NotImplementedException();
    }
}