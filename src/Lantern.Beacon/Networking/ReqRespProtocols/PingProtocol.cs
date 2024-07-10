using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class PingProtocol(IPeerState peerState, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<PingProtocol>();
    public string Id => "/eth2/beacon_chain/req/ping/1/ssz_snappy";

    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        var ping = Ping.CreateFrom(peerState.MetaData.SeqNumber);
        var sszData = Ping.Serialize(ping);
        var payload = ReqRespHelpers.EncodeRequest(sszData);
        var rawData = new ReadOnlySequence<byte>(payload);

        _logger?.LogDebug("Sending ping to {PeerId} with SeqNumber {Value} and data {Data}", context.RemotePeer.Address.Get<P2P>(), peerState.MetaData.SeqNumber, Convert.ToHexString(payload));

        await downChannel.WriteAsync(rawData);
        var receivedData = new List<byte[]>();

        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }
        
        if (receivedData.Count == 0 || receivedData[0] == null || receivedData[0].Length == 0)
        {
            // Log that we received an empty or null response
            _logger?.LogWarning("Received an empty or null response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }

        var flatData = receivedData.SelectMany(x => x).ToArray();
        var result = ReqRespHelpers.DecodeResponse(flatData);
        
        if(result.Item2 != ResponseCodes.Success)
        {
            _logger?.LogError("Failed to decode ping response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }
        
        var pingResponse = Ping.Deserialize(result.Item1);
        
        _logger?.LogDebug("Received pong from {PeerId} with seq number {SeqNumber}", context.RemotePeer.Address.Get<P2P>(), pingResponse.SeqNumber);
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Listening for ping request from {PeerId}", context.RemotePeer.Address);
        
        var receivedData = new List<byte[]>();
        
        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }
        
        var flatData = receivedData.SelectMany(x => x).ToArray();
        var result = ReqRespHelpers.DecodeRequest(flatData);

        if (result == null)
        {
            _logger?.LogError("Failed to decode ping request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }

        var responseCode = (int)ResponseCodes.Success;
        var ping = Ping.CreateFrom(peerState.MetaData.SeqNumber);
        var sszData = Ping.Serialize(ping);
        var payload = ReqRespHelpers.EncodeResponse(sszData, (ResponseCodes)responseCode);
        var rawData = new ReadOnlySequence<byte>(payload);
        
        await downChannel.WriteAsync(rawData);
        
        _logger?.LogDebug("Sent pong to {PeerId} with SeqNumber {Value} and data {Data}", context.RemotePeer.Address.Get<P2P>(), peerState.MetaData.SeqNumber, Convert.ToHexString(payload));
    }
}