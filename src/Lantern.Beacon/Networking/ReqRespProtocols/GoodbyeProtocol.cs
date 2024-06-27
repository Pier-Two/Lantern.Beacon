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

public class GoodbyeProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<GoodbyeProtocol>();
    public string Id => "/eth2/beacon_chain/req/goodbye/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Sending goodbye to {PeerId}", context.RemotePeer.Address.Get<P2P>());
        
        var goodbye = Goodbye.CreateFrom((ulong)GoodbyeReasonCodes.ClientShutdown);
        var sszData = Goodbye.Serialize(goodbye);
        var payload = ReqRespHelpers.EncodeRequest(sszData);
        var rawData = new ReadOnlySequence<byte>(payload);
        
        await downChannel.WriteAsync(rawData);
        var receivedData = new List<byte[]>();
        
        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }
        
        var flatData = receivedData.SelectMany(x => x).ToArray();
        
        if(flatData.Length == 0)
        {
            _logger?.LogWarning("Did not receive goodbye response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }
        
        var result = ReqRespHelpers.DecodeResponse(flatData);
        
        if(result.Item2 != ResponseCodes.Success)
        {
            _logger?.LogError("Failed to decode ping response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }
        
        var goodbyeResponse = Goodbye.Deserialize(result.Item1);
        
        _logger?.LogInformation("Received goodbye response from {PeerId} with reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (GoodbyeReasonCodes)goodbyeResponse.Reason);
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Listening for goodbye response from {PeerId}", context.RemotePeer.Address);
        
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
        
        var goodbyeResponse = Goodbye.Deserialize(result);
        _logger?.LogInformation("Received goodbye response from {PeerId} with reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (GoodbyeReasonCodes)goodbyeResponse.Reason);
        
        var responseCode = (int)ResponseCodes.Success;
        var goodbye = Goodbye.CreateFrom((ulong)GoodbyeReasonCodes.ClientShutdown);
        var sszData = Goodbye.Serialize(goodbye);
        var payload = ReqRespHelpers.EncodeResponse(sszData, (ResponseCodes)responseCode);
        var rawData = new ReadOnlySequence<byte>(payload);
        
        await downChannel.WriteAsync(rawData);
        
        _logger?.LogInformation("Sent goodbye response to {PeerId} with reason {Reason}", context.RemotePeer.Address.Get<P2P>(), GoodbyeReasonCodes.ClientShutdown);
    }
}