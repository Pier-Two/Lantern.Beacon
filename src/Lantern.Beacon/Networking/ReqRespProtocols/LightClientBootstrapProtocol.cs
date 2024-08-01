using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
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

public class LightClientBootstrapProtocol(ISyncProtocol syncProtocol, ILiteDbService liteDbService, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientBootstrapProtocol>();
    
    public string Id => "/eth2/beacon_chain/req/light_client_bootstrap/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        try
        {
            var trustedBlockRoot = syncProtocol.Options.TrustedBlockRoot;
            var request = LightClientBootstrapRequest.CreateFrom(trustedBlockRoot);
            var sszData = LightClientBootstrapRequest.Serialize(request);
            var payload = ReqRespHelpers.EncodeRequest(sszData);
            var rawData = new ReadOnlySequence<byte>(payload);
        
            await downChannel.WriteAsync(rawData);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Config.RespTimeout));
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

            if (flatData[0] == (byte)ResponseCodes.ResourceUnavailable || flatData[0] == (byte)ResponseCodes.InvalidRequest || flatData[0] == (byte)ResponseCodes.ServerError)
            {
                _logger?.LogDebug("Failed to handle light client bootstrap response from {PeerId} due to reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
                await downChannel.CloseAsync();
                return;
            }

            var result = ReqRespHelpers.DecodeResponseChunk(flatData);
            var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);
            
            switch (forkType)
            {
                case ForkType.Deneb:
                    var denebLightClientBootstrap = DenebLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var denebResult = syncProtocol.InitialiseStoreFromDenebBootstrap(syncProtocol.Options.TrustedBlockRoot, denebLightClientBootstrap);

                    if (denebResult)
                    {
                        syncProtocol.SetActiveFork(ForkType.Deneb);
                        liteDbService.Store(nameof(DenebLightClientBootstrap), denebLightClientBootstrap);
                        _logger?.LogInformation("Processed light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    break;
                case ForkType.Capella:
                    var capellaLightClientBootstrap = CapellaLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var capellaResult = syncProtocol.InitialiseStoreFromCapellaBootstrap(syncProtocol.Options.TrustedBlockRoot, capellaLightClientBootstrap);

                    if (capellaResult)
                    {
                        syncProtocol.SetActiveFork(ForkType.Capella);
                        liteDbService.Store(nameof(CapellaLightClientBootstrap), capellaLightClientBootstrap);
                        _logger?.LogInformation("Processed light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    break;
                case ForkType.Bellatrix:
                    var bellatrixLightClientBootstrap = AltairLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var bellatrixResult = syncProtocol.InitialiseStoreFromAltairBootstrap(syncProtocol.Options.TrustedBlockRoot, bellatrixLightClientBootstrap);

                    if (bellatrixResult)
                    {
                        syncProtocol.SetActiveFork(ForkType.Bellatrix);
                        liteDbService.Store(nameof(AltairLightClientBootstrap), bellatrixLightClientBootstrap);
                        _logger?.LogInformation("Processed light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    break;
                case ForkType.Altair:
                    var altairLightClientBootstrap = AltairLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    var altairResult = syncProtocol.InitialiseStoreFromAltairBootstrap(syncProtocol.Options.TrustedBlockRoot, altairLightClientBootstrap);

                    if (altairResult)
                    {
                        syncProtocol.SetActiveFork(ForkType.Altair);
                        liteDbService.Store(nameof(AltairLightClientBootstrap), altairLightClientBootstrap);
                        _logger?.LogInformation("Processed light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    else
                    {
                        _logger?.LogError("Failed to process light client bootstrap from {PeerId} for fork {ForkType}", context.RemotePeer.Address.Get<P2P>(), forkType);
                    }
                    break;
                case ForkType.Phase0:
                    _logger?.LogError("Received light client bootstrap response with unexpected fork type from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    await downChannel.CloseAsync();
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for light client bootstrap response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for light client bootstrap response from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Received light client bootstrap request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
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
                _logger?.LogError("Failed to decode light client bootstrap request from {PeerId}",
                    context.RemotePeer.Address.Get<P2P>());
                await downChannel.CloseAsync();
                return;
            }

            var request = LightClientBootstrapRequest.Deserialize(result);

            _logger?.LogInformation(
                "Received light client bootstrap request from {PeerId} with trustedBlockRoot {trustedBlockRoot}",
                context.RemotePeer.Address.Get<P2P>(), Convert.ToHexString(request.BlockRoot));

            var currentVersion = Phase0Helpers.ComputeForkVersion(
                Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            var forkDigest = Phase0Helpers.ComputeForkDigest(currentVersion, syncProtocol.Options);
            var requestedRoot = Convert.ToHexString(request.BlockRoot);
            var response = liteDbService.FetchByPredicate<DenebLightClientBootstrap>(nameof(DenebLightClientBootstrap),
                x => x.Header.Beacon.HashTreeRootString == requestedRoot);

            if (response == null)
            {
                _logger?.LogDebug(
                    "No light client bootstrap available for block root {trustedBlockRoot}",
                    Convert.ToHexString(request.BlockRoot));
                var encodedResponse = ReqRespHelpers.EncodeResponse([], forkDigest, ResponseCodes.ResourceUnavailable);
                var rawData = new ReadOnlySequence<byte>(encodedResponse);

                await downChannel.WriteAsync(rawData);
            }
            else
            {
                var sszData = DenebLightClientBootstrap.Serialize(response, syncProtocol.Options.Preset);
                var encodedResponse = ReqRespHelpers.EncodeResponse(sszData, forkDigest, ResponseCodes.Success);
                var rawData = new ReadOnlySequence<byte>(encodedResponse);

                await downChannel.WriteAsync(rawData);
                _logger?.LogDebug("Sent light client bootstrap response to {PeerId}",
                    context.RemotePeer.Address.Get<P2P>());
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Timeout occured while listening for light client bootstrap request from {PeerId}",
                context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("Error occured while listening for light client bootstrap request from {PeerId}. Exception: {Message}", context.RemotePeer.Address.Get<P2P>(), ex.Message);
            await downChannel.CloseAsync();
        }
    }
}