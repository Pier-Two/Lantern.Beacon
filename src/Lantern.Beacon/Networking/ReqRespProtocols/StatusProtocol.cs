using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class StatusProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<StatusProtocol>();
    public string Id => "/eth2/beacon_chain/req/status/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        var forkDigest = BeaconClientUtility.GetForkDigestBytes(syncProtocol.Options);
        var finalisedRoot = syncProtocol.CapellaLightClientStore.FinalizedHeader.GetHashTreeRoot(syncProtocol.Options.Preset);
        var finalizedEpoch = Phase0Helpers.ComputeEpochAtSlot(syncProtocol.CapellaLightClientStore.FinalizedHeader.Beacon.Slot);
        var headRoot = syncProtocol.CapellaLightClientStore.OptimisticHeader.GetHashTreeRoot(syncProtocol.Options.Preset);
        var headSlot = syncProtocol.CapellaLightClientStore.OptimisticHeader.Beacon.Slot;
        var localStatus = Status.CreateFrom(forkDigest, finalisedRoot, finalizedEpoch, headRoot, headSlot);
        var sszData = Status.Serialize(localStatus);
        var payload = ReqRespHelpers.EncodeRequest(sszData);
        var rawData = new ReadOnlySequence<byte>(payload);
        
        _logger?.LogInformation("Sending status request to {PeerId} with forkDigest={ForkDigest}, finalizedRoot={FinalizedRoot}, finalizedEpoch={FinalizedEpoch}, headRoot={HeadRoot}, headSlot={HeadSlot}", context.RemotePeer.Address.Get<P2P>(), Convert.ToHexString(localStatus.ForkDigest), Convert.ToHexString(localStatus.FinalizedRoot), localStatus.FinalizedEpoch, Convert.ToHexString(localStatus.HeadRoot), localStatus.HeadSlot);
        
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
        
        if (flatData[0] == (byte)ResponseCodes.ResourceUnavailable || flatData[0] == (byte)ResponseCodes.InvalidRequest || flatData[0] == (byte)ResponseCodes.ServerError)
        {
            _logger?.LogInformation("Failed to handle status response from {PeerId} due to reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
            await downChannel.CloseAsync();
            return;
        }
        
        var result = ReqRespHelpers.DecodeResponse(flatData);
        
        if(result.Item2 != ResponseCodes.Success)
        {
            _logger?.LogError("Failed to decode status response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }

        var statusResponse = Status.Deserialize(result.Item1);
        
        _logger?.LogInformation("Received status response from {PeerId} with forkDigest={forkDigest}, finalizedRoot={finalizedRoot}, finalizedEpoch={finalizedEpoch}, headRoot={headRoot}, headSlot={headSlot}", context.RemotePeer.Address.Get<P2P>(), 
            Convert.ToHexString(statusResponse.ForkDigest), Convert.ToHexString(statusResponse.FinalizedRoot), statusResponse.FinalizedEpoch, Convert.ToHexString(statusResponse.HeadRoot), statusResponse.HeadSlot);
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogDebug("Listening for status request from {PeerId}", context.RemotePeer.Address);
        
        var receivedData = new List<byte[]>();

        try
        {
            await foreach (var readOnlySequence in downChannel.ReadAllAsync())
            {
                Console.WriteLine("Received data: " + Convert.ToHexString(readOnlySequence.ToArray()));
                receivedData.Add(readOnlySequence.ToArray());
            }
        
            var flatData = receivedData.SelectMany(x => x).ToArray();
            var result = ReqRespHelpers.DecodeRequest(flatData);

            if (result == null)
            {
                _logger?.LogError("Failed to decode status request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }
            
            var statusResponse = Status.Deserialize(result);
            _logger?.LogInformation("Received status request from {PeerId} with forkDigest={forkDigest}, finalizedRoot={finalizedRoot}, finalizedEpoch={finalizedEpoch}, headRoot={headRoot}, headSlot={headSlot}", context.RemotePeer.Address.Get<P2P>(), 
                Convert.ToHexString(statusResponse.ForkDigest), Convert.ToHexString(statusResponse.FinalizedRoot), statusResponse.FinalizedEpoch, Convert.ToHexString(statusResponse.HeadRoot), statusResponse.HeadSlot);
            
            var forkDigest = BeaconClientUtility.GetForkDigestBytes(syncProtocol.Options);
            var finalisedRoot = syncProtocol.CapellaLightClientStore.FinalizedHeader.GetHashTreeRoot(syncProtocol.Options.Preset);
            var finalizedEpoch = Phase0Helpers.ComputeEpochAtSlot(syncProtocol.CapellaLightClientStore.FinalizedHeader.Beacon.Slot);
            var headRoot = syncProtocol.CapellaLightClientStore.OptimisticHeader.GetHashTreeRoot(syncProtocol.Options.Preset);
            var headSlot = syncProtocol.CapellaLightClientStore.OptimisticHeader.Beacon.Slot;
            var localStatus = Status.CreateFrom(forkDigest, finalisedRoot, finalizedEpoch, headRoot, headSlot);
            var sszData = Status.Serialize(localStatus);
            var payload = ReqRespHelpers.EncodeResponse(sszData, ResponseCodes.Success);
            var rawData = new ReadOnlySequence<byte>(payload);
            
            await downChannel.WriteAsync(rawData);
            
            _logger?.LogInformation("Sent status response to {PeerId} with data {Data}", context.RemotePeer.Address.Get<P2P>(), Convert.ToHexString(payload));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while listening for status request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
        }
    }
}