using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClientPeerFactory : PeerFactory
{
    public BeaconClientPeerFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override async Task ConnectedTo(IRemotePeer peer, bool isDialer)
    {
        await peer.DialAsync<PeerIdentifyProtocol>();
    }

    public override ILocalPeer Create(Identity? identity = null, Multiaddress? localAddr = null)
    {
        identity ??= new Identity();
        localAddr ??= $"/ip4/0.0.0.0/tcp/0/p2p/{identity.PeerId}";
        if (localAddr.Get<P2P>() is null)
        {
            localAddr.Add<P2P>(identity.PeerId.ToString());
        }
        return base.Create(identity, localAddr);
    }
}