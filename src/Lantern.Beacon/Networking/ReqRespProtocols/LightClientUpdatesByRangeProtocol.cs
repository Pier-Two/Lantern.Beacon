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

public class LightClientUpdatesByRangeProtocol(ISyncProtocol syncProtocol, ILiteDbService liteDbService, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientUpdatesByRangeProtocol>();
    public string Id => "/eth2/beacon_chain/req/light_client_updates_by_range/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        try
        {
            var request = syncProtocol.LightClientUpdatesByRangeRequest;
            var sszData = LightClientUpdatesByRangeRequest.Serialize(request);
            var payload = ReqRespHelpers.EncodeRequest(sszData);
            var rawData = new ReadOnlySequence<byte>(payload);

            await downChannel.WriteAsync(rawData);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.RespTimeout));
            var receivedData = new List<byte[]>();
            var responses = new List<byte[]>();
            var index = 0;

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

            var responseCode = receivedData[0][0];

            if (responseCode is (byte)ResponseCodes.ResourceUnavailable or (byte)ResponseCodes.InvalidRequest
                or (byte)ResponseCodes.ServerError)
            {
                _logger?.LogInformation(
                    "Failed to handle light client update response from {PeerId} due to reason {Reason}",
                    context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)receivedData[0][0]);
                await downChannel.CloseAsync();
                return;
            }

            foreach (var data in receivedData) 
            {
                if (data is [(byte)ResponseCodes.Success] || data[0] == (byte)ResponseCodes.Success) 
                {
                    responses.Add(data);
                    index++;
                } 
                else 
                {
                    if (index > 0) 
                    {
                        responses[index - 1] = responses[index - 1].Concat(data).ToArray();
                    } 
                    else 
                    {
                        _logger?.LogWarning("Received non-success data without prior success response. PeerId: {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    }
                }
            }

            foreach (var responseChunk in responses)
            {
                var result = ReqRespHelpers.DecodeResponseChunk(responseChunk);
                var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);
                var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);

                switch (forkType)
                {
                    case ForkType.Deneb:
                        var denebLightClientUpdate =
                            DenebLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        var denebResult = DenebProcessors.ProcessLightClientUpdate(syncProtocol.DenebLightClientStore,
                            denebLightClientUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);

                        if (denebResult)
                        {
                            liteDbService.Store(nameof(DenebLightClientUpdate), denebLightClientUpdate);
                            liteDbService.StoreOrUpdate(nameof(DenebLightClientStore),
                                syncProtocol.DenebLightClientStore);

                            _logger?.LogInformation("Processed light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }
                        else
                        {
                            _logger?.LogError("Failed to process light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }

                        break;
                    case ForkType.Capella:
                        var capellaLightClientUpdate =
                            CapellaLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        var capellaResult = CapellaProcessors.ProcessLightClientUpdate(
                            syncProtocol.CapellaLightClientStore, capellaLightClientUpdate, currentSlot,
                            syncProtocol.Options, syncProtocol.Logger);

                        if (capellaResult)
                        {
                            liteDbService.Store(nameof(CapellaLightClientUpdate), capellaLightClientUpdate);
                            liteDbService.StoreOrUpdate(nameof(CapellaLightClientStore),
                                syncProtocol.CapellaLightClientStore);
                            _logger?.LogInformation("Processed light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }
                        else
                        {
                            _logger?.LogError("Failed to process light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }

                        break;
                    case ForkType.Bellatrix:
                        var bellatrixLightClientUpdate =
                            AltairLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        var bellatrixResult = AltairProcessors.ProcessLightClientUpdate(
                            syncProtocol.AltairLightClientStore, bellatrixLightClientUpdate, currentSlot,
                            syncProtocol.Options, syncProtocol.Logger);

                        if (bellatrixResult)
                        {
                            liteDbService.Store(nameof(AltairLightClientUpdate), bellatrixLightClientUpdate);
                            liteDbService.StoreOrUpdate(nameof(AltairLightClientStore),
                                syncProtocol.AltairLightClientStore);
                            _logger?.LogInformation("Processed light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }
                        else
                        {
                            _logger?.LogError("Failed to process light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }

                        break;
                    case ForkType.Altair:
                        var altairLightClientUpdate =
                            AltairLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        var altairResult = AltairProcessors.ProcessLightClientUpdate(
                            syncProtocol.AltairLightClientStore, altairLightClientUpdate, currentSlot,
                            syncProtocol.Options, syncProtocol.Logger);

                        if (altairResult)
                        {
                            liteDbService.Store(nameof(AltairLightClientUpdate), altairLightClientUpdate);
                            liteDbService.StoreOrUpdate(nameof(AltairLightClientStore),
                                syncProtocol.AltairLightClientStore);
                            _logger?.LogInformation("Processed light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }
                        else
                        {
                            _logger?.LogError("Failed to process light client update response from {PeerId}",
                                context.RemotePeer.Address.Get<P2P>());
                        }

                        break;
                    case ForkType.Phase0:
                        _logger?.LogError(
                            "Received light client update response with unexpected fork type from {PeerId}",
                            context.RemotePeer.Address.Get<P2P>());
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for light client update response from {PeerId}",
                context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for light client update response from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex);
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Received light client update request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
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
                _logger?.LogDebug("Failed to decode light client update request from {PeerId}",
                    context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }

            var request = LightClientUpdatesByRangeRequest.Deserialize(result);

            for (var i = request.StartPeriod; i < request.Count + request.StartPeriod; i++)
            {
                var currentVersion = Phase0Helpers.ComputeForkVersion(
                    Phase0Helpers.ComputeEpochAtSlot(
                        Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
                var forkDigest = Phase0Helpers.ComputeForkDigest(currentVersion, syncProtocol.Options);
                var response = liteDbService.FetchByPredicate<DenebLightClientUpdate>(nameof(DenebLightClientUpdate),
                    x => x.SyncCommitteePeriod == request.StartPeriod);

                if (response == null)
                {
                    _logger?.LogInformation(
                        "No light client update available for sync period {startPeriod} and count {count}",
                        request.StartPeriod, request.Count);

                    var sszData = ErrorMessage.Serialize(ErrorMessage.CreateFrom($"Light client update is unavailable for sync period {request.StartPeriod}"));
                    var encodedResponse =
                        ReqRespHelpers.EncodeResponse(sszData, ResponseCodes.ResourceUnavailable);
                    var rawData = new ReadOnlySequence<byte>(encodedResponse);

                    await downChannel.WriteAsync(rawData);
                }
                else
                {
                    var sszData = DenebLightClientUpdate.Serialize(response, syncProtocol.Options.Preset);
                    var encodedResponse = ReqRespHelpers.EncodeResponse(sszData, forkDigest, ResponseCodes.Success);
                    var rawData = new ReadOnlySequence<byte>(encodedResponse);

                    await downChannel.WriteAsync(rawData);
                    _logger?.LogInformation(
                        "Sent light client update response to {PeerId} for sync period {startPeriod}",
                        context.RemotePeer.Address.Get<P2P>(), request.StartPeriod);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for light client update request from {PeerId}",
                context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for light client update request from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex);
            await downChannel.CloseAsync();
        }
    }
}