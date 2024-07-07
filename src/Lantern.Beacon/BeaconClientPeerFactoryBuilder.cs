using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;
using Nethermind.Libp2p.Protocols.Pubsub;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon;

public class BeaconClientPeerFactoryBuilder : PeerFactoryBuilderBase<BeaconClientPeerFactoryBuilder, Libp2pPeerFactory>,
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
                .Over<YamuxProtocol>();//.Or<MplexProtocol>();

        return
            Over<MultiaddressBasedSelectorProtocol>()
                .Over<QuicProtocol>().Or(tcpStack)
                .Over<MultistreamProtocol>()
                .AddAppLayerProtocol<IdentifyProtocol>()
                .AddAppLayerProtocol<GossipsubProtocol>()
                .AddAppLayerProtocol<FloodsubProtocol>();
    }
}
