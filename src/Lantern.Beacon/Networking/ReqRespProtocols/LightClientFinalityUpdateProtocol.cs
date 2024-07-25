using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Basic;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class LightClientFinalityUpdateProtocol(ISyncProtocol syncProtocol, ILiteDbService liteDbService, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientFinalityUpdateProtocol>();
    public string Id => "/eth2/beacon_chain/req/light_client_finality_update/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
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
        
        try
        {
            
            if (flatData[0] == (byte)ResponseCodes.ResourceUnavailable || flatData[0] == (byte)ResponseCodes.InvalidRequest || flatData[0] == (byte)ResponseCodes.ServerError)
            {
                _logger?.LogInformation("Failed to handle light client finality update response from {PeerId} due to reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
                await downChannel.CloseAsync();
                return;
            }
            
            var result = ReqRespHelpers.DecodeResponseChunk(flatData);
            var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
        
            switch (forkType)
            {
                case ForkType.Deneb:
                    var denebLightClientFinalityUpdate = DenebLightClientFinalityUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    DenebProcessors.ProcessLightClientFinalityUpdate(syncProtocol.DenebLightClientStore, denebLightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    liteDbService.StoreOrUpdate(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
                    _logger?.LogInformation("Processed light client finality update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Capella:
                    var capellaLightClientFinalityUpdate = CapellaLightClientFinalityUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    CapellaProcessors.ProcessLightClientFinalityUpdate(syncProtocol.CapellaLightClientStore, capellaLightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    liteDbService.StoreOrUpdate(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
                    _logger?.LogInformation("Processed light client finality update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Bellatrix:
                    var bellatrixLightClientFinalityUpdate = AltairLightClientFinalityUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    AltairProcessors.ProcessLightClientFinalityUpdate(syncProtocol.AltairLightClientStore, bellatrixLightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    liteDbService.StoreOrUpdate(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
                    _logger?.LogInformation("Processed light client finality update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Altair:
                    var altairLightClientFinalityUpdate = AltairLightClientFinalityUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    AltairProcessors.ProcessLightClientFinalityUpdate(syncProtocol.AltairLightClientStore, altairLightClientFinalityUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    liteDbService.StoreOrUpdate(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
                    _logger?.LogInformation("Processed light client finality update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Phase0:
                    _logger?.LogError("Received light client finality response with unexpected fork type from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to receieve light client finality update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Received light client finality update request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
        
        try
        {
            var currentVersion = Phase0Helpers.ComputeForkVersion(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            var forkDigest = Phase0Helpers.ComputeForkDigest(currentVersion, syncProtocol.Options);
            var response = syncProtocol.CurrentLightClientFinalityUpdate;

            if (response == null || response.Equals(DenebLightClientFinalityUpdate.CreateDefault()))
            {
                _logger?.LogInformation("No light client finality update available to send to {PeerId}", context.RemotePeer.Address.Get<P2P>());
                var encodedResponse = ReqRespHelpers.EncodeResponse([], forkDigest,ResponseCodes.ResourceUnavailable);
                var rawData = new ReadOnlySequence<byte>(encodedResponse);
                
                await downChannel.WriteAsync(rawData);
            }
            else
            {
                _logger?.LogInformation("Sending light client finality update response to {PeerId}", context.RemotePeer.Address.Get<P2P>());
                var sszData = DenebLightClientFinalityUpdate.Serialize(response, syncProtocol.Options.Preset);
                var encodedResponse = ReqRespHelpers.EncodeResponse(sszData, forkDigest, ResponseCodes.Success);
                var rawData = new ReadOnlySequence<byte>(encodedResponse);

                await downChannel.WriteAsync(rawData);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to handle light client finality update request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
    }
}