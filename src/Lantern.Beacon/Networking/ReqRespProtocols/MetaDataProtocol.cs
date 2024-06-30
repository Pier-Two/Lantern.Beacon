using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types.Phase0;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using SszSharp;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class MetaDataProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<MetaDataProtocol>();
    
    public string Id => "/eth2/beacon_chain/req/metadata/2/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        var receivedData = new List<byte[]>();

        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }

        var flatData = receivedData.SelectMany(x => x).ToArray();
        
        if (flatData[0] == (byte)ResponseCodes.ResourceUnavailable || flatData[0] == (byte)ResponseCodes.InvalidRequest || flatData[0] == (byte)ResponseCodes.ServerError)
        {
            _logger?.LogInformation("Failed to handle metadata response from {PeerId} due to reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
            await downChannel.CloseAsync();
            return;
        }
        
        var result = ReqRespHelpers.DecodeResponse(flatData);
        
        if(result.Item2 != ResponseCodes.Success)
        {
            _logger?.LogError("Failed to decode metadata response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }
        
        var metaDataResponse = MetaData.Deserialize(result.Item1);
        
        _logger?.LogDebug("Received metadata response from {PeerId} with seq number {SeqNumber} and attnets {Attnets}", 
            context.RemotePeer.Address.Get<P2P>(), 
            metaDataResponse.SeqNumber, 
            Convert.ToHexString(metaDataResponse.Attnets.Select(b => b ? (byte)1 : (byte)0).ToArray()));
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Listening for MetaData request from {PeerId}", context.RemotePeer.Address);

        var rawData = new ReadOnlySequence<byte>();

        try
        {
            var responseCode = (int)ResponseCodes.Success;
            var metaData = syncProtocol.MetaData;
            var sszData = MetaData.Serialize(metaData);
            var payload = ReqRespHelpers.EncodeResponse(sszData, (ResponseCodes)responseCode);
            rawData = new ReadOnlySequence<byte>(payload);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to encode MetaData request from {PeerId}. Exception: {Message}", 
                context.RemotePeer.Address.Get<P2P>(), e.Message);
            return;
        }
        
        await downChannel.WriteAsync(rawData);
        _logger?.LogDebug("Sent MetaData to {PeerId} with data {Data}", context.RemotePeer.Address.Get<P2P>(), Convert.ToHexString(rawData.ToArray()));
    }
}