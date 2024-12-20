using Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;
using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Lantern.Beacon.Networking.Libp2pProtocols.Mplex;
using Lantern.Beacon.Networking.Libp2pProtocols.Secp256k1Noise;
using Lantern.Beacon.Networking.Libp2pProtocols.TcpProtocol;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;
using PingProtocol = Lantern.Beacon.Networking.ReqRespProtocols.PingProtocol;

namespace Lantern.Beacon;

public class BeaconClientPeerFactoryBuilder(IServiceProvider? serviceProvider = default)
    : PeerFactoryBuilderBase<BeaconClientPeerFactoryBuilder, BeaconClientPeerFactory>(serviceProvider),
        ILibp2pPeerFactoryBuilder
{
    private bool enforcePlaintext;

    public ILibp2pPeerFactoryBuilder WithPlaintextEnforced()
    {
        enforcePlaintext = true;
        return this;
    }

    protected override ProtocolStack BuildStack()
    {
        var tcpStack =
            Over<TcpProtocol>()
                .Over<MultistreamProtocol>()
                .Over<Secp256K1NoiseProtocol>()
                .Over<MultistreamProtocol>()
                .Over<MplexProtocol>();

        return
            Over<MultiaddressBasedSelectorProtocol>()
                .Over(tcpStack)
                .Over<MultistreamProtocol>()
                .AddAppLayerProtocol<PeerIdentifyProtocol>()
                .AddAppLayerProtocol<FloodsubProtocol>()
                .AddAppLayerProtocol<GossipsubProtocol>()
                .AddAppLayerProtocol<GossipsubProtocolV11>()
                .AddAppLayerProtocol<GossipsubProtocolV12>()
                .AddAppLayerProtocol<PingProtocol>()
                .AddAppLayerProtocol<StatusProtocol>()
                .AddAppLayerProtocol<MetaDataProtocol>()
                .AddAppLayerProtocol<GoodbyeProtocol>()
                .AddAppLayerProtocol<LightClientBootstrapProtocol>()
                .AddAppLayerProtocol<LightClientFinalityUpdateProtocol>()
                .AddAppLayerProtocol<LightClientOptimisticUpdateProtocol>()
                .AddAppLayerProtocol<LightClientUpdatesByRangeProtocol>();
    }
}
