using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Stack;

namespace Lantern.Beacon.Networking.Libp2pProtocols.RawProtocolSelector;

public class RawProtocolSelector(ILoggerFactory? loggerFactory = null) : SymmetricProtocol, IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<RawProtocolSelector>();

    public string Id => "multiaddr-select";

    protected override async Task ConnectAsync(IChannel _, IChannelFactory? channelFactory, IPeerContext context, bool isListener)
    {
        IProtocol protocol = null!;
        // TODO: deprecate quic
        if (context.LocalPeer.Address.Has<QUICv1>())
        {
            protocol = channelFactory!.SubProtocols.FirstOrDefault(proto => proto.Id == "quic-v1") ?? throw new ApplicationException("QUICv1 is not supported");
        }
        else if (context.LocalPeer.Address.Has<TCP>())
        {
            protocol = channelFactory!.SubProtocols.FirstOrDefault(proto => proto.Id == "ip-tcp") ?? throw new ApplicationException("TCP is not supported");
        }
        else if (context.LocalPeer.Address.Has<QUIC>())
        {
            throw new ApplicationException("QUIC is not supported. Use QUICv1 instead.");
        }
        else
        {
            throw new NotImplementedException($"No transport protocol found for the given address: {context.LocalPeer.Address}");
        }
        
        await (isListener
            ? channelFactory.SubListenAndBind(_, context, protocol)
            : channelFactory.SubDialAndBind(_, context, protocol));
    }
}