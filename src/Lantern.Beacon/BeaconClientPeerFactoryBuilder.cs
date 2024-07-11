using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Lantern.Beacon.Networking.Libp2pProtocols.Mplex;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;
using Nethermind.Libp2p.Protocols.Pubsub;

namespace Lantern.Beacon;

public class BeaconClientPeerFactoryBuilder : PeerFactoryBuilderBase<BeaconClientPeerFactoryBuilder, BeaconClientPeerFactory>,
    ILibp2pPeerFactoryBuilder
{
    private bool enforcePlaintext;

    public ILibp2pPeerFactoryBuilder WithPlaintextEnforced()
    {
        enforcePlaintext = true;
        return this;
    }

    public BeaconClientPeerFactoryBuilder(IServiceProvider? serviceProvider = default) : base(serviceProvider)
    {
    }

    protected override ProtocolStack BuildStack()
    {
        ProtocolStack tcpStack =
            Over<IpTcpProtocol>()
                .Over<MultistreamProtocol>()
                .Over<NoiseProtocol>()
                .Over<MultistreamProtocol>()
                .Over<YamuxProtocol>().Or<MplexProtocol>(); 

        return
            Over<MultiaddressBasedSelectorProtocol>()
                .Over(tcpStack)
                .Over<MultistreamProtocol>()
                .AddAppLayerProtocol<PeerIdentifyProtocol>()
                .AddAppLayerProtocol<GossipsubProtocol>()
                .AddAppLayerProtocol<GossipsubProtocolV11>()
                .AddAppLayerProtocol<FloodsubProtocol>();
    }
}
