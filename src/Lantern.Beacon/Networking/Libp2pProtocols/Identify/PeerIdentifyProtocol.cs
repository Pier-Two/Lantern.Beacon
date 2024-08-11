using System.Net;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Dto;
using Nethermind.Libp2p.Protocols;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Identify;

public class PeerIdentifyProtocol(IPeerState peerState, IdentifyProtocolSettings? settings = null, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly string _agentVersion = settings?.AgentVersion ?? IdentifyProtocolSettings.Default.AgentVersion!;
    private readonly string _protocolVersion = settings?.ProtocolVersion ?? IdentifyProtocolSettings.Default.ProtocolVersion!;
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<PeerIdentifyProtocol>();
    
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
        
        peerState.PeerProtocols.TryAdd(context.RemotePeer.Address.GetPeerId(), identity.Protocols);
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Listen");
        
        var listenAddress = context.LocalEndpoint;
        listenAddress.Remove<P2P>();
        
        var observedAddress = context.RemoteEndpoint;
        observedAddress.Remove<P2P>();
        
        Nethermind.Libp2p.Protocols.Identify.Dto.Identify identify = new()
        {
            ProtocolVersion = _protocolVersion,
            AgentVersion = _agentVersion,
            PublicKey = context.LocalPeer.Identity.PublicKey.ToByteString(),
            ListenAddrs = { ByteString.CopyFrom(GetFilteredBytes(listenAddress)) },
            ObservedAddr = ByteString.CopyFrom(GetFilteredBytes(observedAddress)), 
            Protocols = { peerState.AppLayerProtocols.Select(p => p.Id) }
        };
        
        var ar = new byte[identify.CalculateSize()];
        identify.WriteTo(ar);

        await downChannel.WriteSizeAndDataAsync(ar);
        _logger?.LogDebug("Sent peer info {identify}", identify);
    }
    
    private static byte[] GetFilteredBytes(Multiaddress multiaddress) 
    {
        multiaddress.Remove<P2P>();
        
        var bytes = multiaddress.ToBytes();
        var len = bytes.Length;
        
        if (len < 4) 
            return bytes;
        
        if (bytes[len - 4] == 0 && bytes[len - 3] == 0) 
        {
            var filteredBytes = new byte[len - 2];
            Array.Copy(bytes, 0, filteredBytes, 0, len - 4);
            filteredBytes[len - 4] = bytes[len - 2];
            filteredBytes[len - 3] = bytes[len - 1];
            return filteredBytes;
        }
        
        return bytes;
    }
}