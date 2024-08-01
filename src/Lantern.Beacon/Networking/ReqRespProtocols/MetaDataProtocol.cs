using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using SszSharp;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class MetaDataProtocol(IPeerState peerState, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<MetaDataProtocol>();
    
    public string Id => "/eth2/beacon_chain/req/metadata/2/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        var receivedData = new List<byte[]>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.RespTimeout));

        try
        {
            await foreach (var readOnlySequence in downChannel.ReadAllAsync(cts.Token))
            {
                receivedData.Add(readOnlySequence.ToArray());
            }

            if (receivedData.Count == 0 || receivedData[0] == null || receivedData[0].Length == 0)
            {
                _logger?.LogDebug("Received an empty or null response from {PeerId}",
                    context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }

            var flatData = receivedData.SelectMany(x => x).ToArray();

            if (flatData[0] == (byte)ResponseCodes.ResourceUnavailable ||
                flatData[0] == (byte)ResponseCodes.InvalidRequest || flatData[0] == (byte)ResponseCodes.ServerError)
            {
                _logger?.LogInformation("Failed to handle metadata response from {PeerId} due to reason {Reason}",
                    context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
                await downChannel.CloseAsync();
                return;
            }

            var result = ReqRespHelpers.DecodeResponse(flatData);

            if (result.Item2 != ResponseCodes.Success)
            {
                _logger?.LogDebug("Failed to decode metadata response from {PeerId}",
                    context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }

            var metaDataResponse = MetaData.Deserialize(result.Item1);

            _logger?.LogDebug(
                "Received metadata response from {PeerId} with seq number {SeqNumber} and attnets {Attnets}",
                context.RemotePeer.Address.Get<P2P>(),
                metaDataResponse.SeqNumber,
                Convert.ToHexString(metaDataResponse.Attnets.Select(b => b ? (byte)1 : (byte)0).ToArray()));
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for MetaData response from {PeerId}",
                context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for MetaData response from {PeerId}. Exception: {Message}", 
                context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Listening for MetaData request from {PeerId}", context.RemotePeer.Address);
        
        try
        {
            var metaData = peerState.MetaData;
            var sszData = MetaData.Serialize(metaData);
            var payload = ReqRespHelpers.EncodeResponse(sszData, ResponseCodes.Success);
            var rawData = new ReadOnlySequence<byte>(payload);
            
            await downChannel.WriteAsync(rawData);
            _logger?.LogInformation("Sent MetaData response to {PeerId}", context.RemotePeer.Address.Get<P2P>());
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for MetaData request from {PeerId}. Exception: {Message}", 
                context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }
}