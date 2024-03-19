using Nethermind.Libp2p.Core.Discovery;
using Multiformats.Address;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Multiformats.Address.Protocols;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol;

namespace Lantern.Beacon.P2P;

public class Discv5DiscoveryProtocol : IDiscoveryProtocol
{
    private readonly Discv5Protocol _discv5Protocol;

    public Func<Multiaddress[], bool>? OnAddPeer { get; set; }
    public Func<Multiaddress[], bool>? OnRemovePeer { get; set; }

    public Discv5DiscoveryProtocol(Discv5Protocol discv5Protocol)
    {
        _discv5Protocol = discv5Protocol ?? throw new ArgumentNullException(nameof(discv5Protocol));
    }

    public async Task DiscoverAsync(Multiaddress localPeerAddr, CancellationToken token = default)
    {
        // Start the Discv5 protocol
        await _discv5Protocol.StartProtocolAsync();

        // Subscribe to node added and removed events
        _discv5Protocol.NodeAdded += NotifyLibp2pAboutNewPeer;
        _discv5Protocol.NodeRemoved += NotifyLibp2pAboutRemovedPeer;

        // Keep the discovery running until cancellation is requested
        token.Register(async () => await _discv5Protocol.StopProtocolAsync());
    }

    private void NotifyLibp2pAboutNewPeer(NodeTableEntry node)
    {
        var multiAddr = ConvertToMultiaddress(node);
        OnAddPeer?.Invoke([multiAddr]);
    }

    private void NotifyLibp2pAboutRemovedPeer(NodeTableEntry node)
    {
        var multiAddr = ConvertToMultiaddress(node);
        OnRemovePeer?.Invoke([multiAddr]);
    }

    private static Multiaddress ConvertToMultiaddress(NodeTableEntry node)
    {
        var ip4Address = node.Record.GetEntry<EntryIp>(EnrEntryKey.Ip);
        var tcpPort = node.Record.GetEntry<EntryTcp>(EnrEntryKey.Tcp);
        var nodeId = node.Record.ToPeerId();
        var multiAddr = new Multiaddress()
            .Add<IP4>(ip4Address.ToString())
            .Add<TCP>(tcpPort)
            .Add<Multiformats.Address.Protocols.P2P>(nodeId);

        return multiAddr;
    }
}