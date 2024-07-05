using Google.Protobuf;
using Lantern.Beacon.Sync;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Dto;
using Nethermind.Libp2p.Protocols;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Identify;

public class CustomIdentifyProtocol(INetworkState networkState, IdentifyProtocolSettings? settings = null, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly string _agentVersion = settings?.AgentVersion ?? IdentifyProtocolSettings.Default.AgentVersion!;
    private readonly string _protocolVersion = settings?.ProtocolVersion ?? IdentifyProtocolSettings.Default.ProtocolVersion!;
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<CustomIdentifyProtocol>();
    
    public string Id => "/ipfs/id/1.0.0";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Dial");

        var identity = await downChannel.ReadPrefixedProtobufAsync(Nethermind.Libp2p.Protocols.Identify.Dto.Identify.Parser);
    
        _logger?.LogDebug("Received peer info: {identify}", identity);
        context.RemotePeer.Identity = new Identity(PublicKey.Parser.ParseFrom(identity.PublicKey));
        
        if (context.RemotePeer.Identity.PublicKey.ToByteString() != identity.PublicKey)
        {
            throw new PeerConnectionException();
        }
        
        networkState.PeerProtocols.TryAdd(context.RemotePeer.Address.GetPeerId(), identity.Protocols);
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Listen");

        Nethermind.Libp2p.Protocols.Identify.Dto.Identify identify = new()
        {
            ProtocolVersion = _protocolVersion,
            AgentVersion = _agentVersion,
            PublicKey = context.LocalPeer.Identity.PublicKey.ToByteString(),
            ListenAddrs = { ByteString.CopyFrom(context.LocalEndpoint.Get<IP>().ToBytes()) },
            ObservedAddr = ByteString.CopyFrom(context.RemoteEndpoint.Get<IP>().ToBytes()), 
            Protocols = { networkState.AppLayerProtocols.Select(p => p.Id) }
        };
        
        var ar = new byte[identify.CalculateSize()];
        identify.WriteTo(ar);

        await downChannel.WriteSizeAndDataAsync(ar);
        _logger?.LogDebug("Sent peer info {identify}", identify);
    }
}