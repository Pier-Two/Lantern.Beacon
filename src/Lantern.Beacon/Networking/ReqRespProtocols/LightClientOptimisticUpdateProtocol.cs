using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Basic;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class LightClientOptimisticUpdateProtocol(ISyncProtocol syncProtocol, ILiteDbService liteDbService, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientOptimisticUpdateProtocol>();
    public string Id => "/eth2/beacon_chain/req/light_client_optimistic_update/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.RespTimeout));

        try
        {
            var receivedData = new List<byte[]>();

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
                _logger?.LogInformation(
                    "Failed to handle light client optimistic update response from {PeerId} due to reason {Reason}",
                    context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
                await downChannel.CloseAsync();
                return;
            }

            var result = ReqRespHelpers.DecodeResponseChunk(flatData);
            var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);

            switch (forkType)
            {
                case ForkType.Deneb:
                    var denebLightClientOptimisticUpdate =
                        DenebLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var denebResult = DenebProcessors.ProcessLightClientOptimisticUpdate(
                        syncProtocol.DenebLightClientStore, denebLightClientOptimisticUpdate, currentSlot,
                        syncProtocol.Options, syncProtocol.Logger);

                    if (denebResult)
                    {
                        liteDbService.ReplaceAllWithItem(nameof(DenebLightClientStore), syncProtocol.DenebLightClientStore);
                        _logger?.LogInformation("Processed light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }

                    await downChannel.CloseAsync();
                    break;
                case ForkType.Capella:
                    var capellaLightClientOptimisticUpdate =
                        CapellaLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var capellaResult = CapellaProcessors.ProcessLightClientOptimisticUpdate(
                        syncProtocol.CapellaLightClientStore, capellaLightClientOptimisticUpdate, currentSlot,
                        syncProtocol.Options, syncProtocol.Logger);

                    if (capellaResult)
                    {
                        liteDbService.ReplaceAllWithItem(nameof(CapellaLightClientStore),
                            syncProtocol.CapellaLightClientStore);
                        _logger?.LogInformation("Processed light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }

                    await downChannel.CloseAsync();
                    break;
                case ForkType.Bellatrix:
                    var bellatrixLightClientOptimisticUpdate =
                        AltairLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var bellatrixResult = AltairProcessors.ProcessLightClientOptimisticUpdate(
                        syncProtocol.AltairLightClientStore, bellatrixLightClientOptimisticUpdate, currentSlot,
                        syncProtocol.Options, syncProtocol.Logger);

                    if (bellatrixResult)
                    {
                        liteDbService.ReplaceAllWithItem(nameof(AltairLightClientStore),
                            syncProtocol.AltairLightClientStore);
                        _logger?.LogInformation("Processed light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }

                    await downChannel.CloseAsync();
                    break;
                case ForkType.Altair:
                    var altairLightClientOptimisticUpdate =
                        AltairLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var altairResult = AltairProcessors.ProcessLightClientOptimisticUpdate(
                        syncProtocol.AltairLightClientStore, altairLightClientOptimisticUpdate, currentSlot,
                        syncProtocol.Options, syncProtocol.Logger);

                    if (altairResult)
                    {
                        liteDbService.ReplaceAllWithItem(nameof(AltairLightClientStore),
                            syncProtocol.AltairLightClientStore);
                        _logger?.LogInformation("Processed light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client optimistic update from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                    }

                    await downChannel.CloseAsync();
                    break;
                case ForkType.Phase0:
                    _logger?.LogError(
                        "Received light client optimistic update response with unexpected fork type from {PeerId}",
                        context.RemotePeer.Address.Get<P2P>());
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for light client optimistic update response from {PeerId}",
                context.RemotePeer.Address.Get<P2P>());
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for light client optimistic update response from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Received light client optimistic update request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
        
        try
        {
            var currentVersion = Phase0Helpers.ComputeForkVersion(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            var forkDigest = Phase0Helpers.ComputeForkDigest(currentVersion, syncProtocol.Options);
            var response = syncProtocol.CurrentLightClientOptimisticUpdate;

            if (response == null || response.Equals(DenebLightClientOptimisticUpdate.CreateDefault()))
            {
                _logger?.LogInformation("No light client optimistic update available to send to {PeerId}", context.RemotePeer.Address.Get<P2P>());
                var sszData = ErrorMessage.Serialize(ErrorMessage.CreateFrom($"Light client optimistic update is unavailable"));
                var encodedResponse = ReqRespHelpers.EncodeResponse(sszData,ResponseCodes.ResourceUnavailable);
                var rawData = new ReadOnlySequence<byte>(encodedResponse);
                
                await downChannel.WriteAsync(rawData);
                await downChannel.CloseAsync();
            }
            else
            {
                var sszData = DenebLightClientOptimisticUpdate.Serialize(response, syncProtocol.Options.Preset);
                var encodedResponse = ReqRespHelpers.EncodeResponse(sszData, forkDigest, ResponseCodes.Success);
                var rawData = new ReadOnlySequence<byte>(encodedResponse);

                await downChannel.WriteAsync(rawData);
                await downChannel.CloseAsync();
                
                _logger?.LogInformation("Sent light client optimistic update response to {PeerId}", context.RemotePeer.Address.Get<P2P>());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for light client optimistic update request from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }
}