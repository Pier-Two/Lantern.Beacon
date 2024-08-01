using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
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
        _logger?.LogInformation("Sending ping to {PeerId}", context.RemotePeer.Address.Get<P2P>());
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.RespTimeout));

        try
        {
            var ping = Ping.CreateFrom(peerState.MetaData.SeqNumber);
            var sszData = Ping.Serialize(ping);
            var payload = ReqRespHelpers.EncodeRequest(sszData);
            var rawData = new ReadOnlySequence<byte>(payload);
            
            await downChannel.WriteAsync(rawData);
            var receivedData = new List<byte[]>();

            await foreach (var readOnlySequence in downChannel.ReadAllAsync(cts.Token))
            {
                receivedData.Add(readOnlySequence.ToArray());
            }
        
            if (receivedData.Count == 0 || receivedData[0] == null || receivedData[0].Length == 0)
            {
                _logger?.LogDebug("Received an empty or null response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }

            var flatData = receivedData.SelectMany(x => x).ToArray();
            var result = ReqRespHelpers.DecodeResponse(flatData);
        
            if(result.Item2 != ResponseCodes.Success)
            {
                _logger?.LogDebug("Failed to decode ping response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }
        
            var pingResponse = Ping.Deserialize(result.Item1);
            _logger?.LogInformation("Received pong response from {PeerId} with seq number {SeqNumber}", context.RemotePeer.Address.Get<P2P>(), pingResponse.SeqNumber);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for ping response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for ping response from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Listening for ping request from {PeerId}", context.RemotePeer.Address);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.TimeToFirstByteTimeout));

        try
        {
            var receivedData = new List<byte[]>();

            await foreach (var readOnlySequence in downChannel.ReadAllAsync(cts.Token))
            {
                receivedData.Add(readOnlySequence.ToArray());
            }

            var flatData = receivedData.SelectMany(x => x).ToArray();
            var result = ReqRespHelpers.DecodeRequest(flatData);

            if (result == null)
            {
                _logger?.LogDebug("Failed to decode ping request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }

            var ping = Ping.CreateFrom(peerState.MetaData.SeqNumber);
            var sszData = Ping.Serialize(ping);
            var payload = ReqRespHelpers.EncodeResponse(sszData, ResponseCodes.Success);
            var rawData = new ReadOnlySequence<byte>(payload);

            await downChannel.WriteAsync(rawData);

            _logger?.LogInformation("Sent pong response to {PeerId}", context.RemotePeer.Address.Get<P2P>());
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for ping request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for ping request from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }
}