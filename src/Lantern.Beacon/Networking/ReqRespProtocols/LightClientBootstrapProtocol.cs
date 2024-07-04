using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Basic;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class LightClientBootstrapProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientBootstrapProtocol>();
    
    public string Id => "/eth2/beacon_chain/req/light_client_bootstrap/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        var trustedBlockRoot = syncProtocol.Options.TrustedBlockRoot;
        var request = LightClientBootstrapRequest.CreateFrom(trustedBlockRoot);
        var sszData = LightClientBootstrapRequest.Serialize(request);
        var payload = ReqRespHelpers.EncodeRequest(sszData);
        var rawData = new ReadOnlySequence<byte>(payload);
        
        await downChannel.WriteAsync(rawData);
        var receivedData = new List<byte[]>();
        
        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }
        
        var flatData = receivedData.SelectMany(x => x).ToArray();
 
        try
        {
            if (flatData[0] == (byte)ResponseCodes.ResourceUnavailable || flatData[0] == (byte)ResponseCodes.InvalidRequest || flatData[0] == (byte)ResponseCodes.ServerError)
            {
                _logger?.LogInformation("Failed to handle light client bootstrap response from {PeerId} due to reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
                await downChannel.CloseAsync();
                return;
            }

            var result = ReqRespHelpers.DecodeResponseChunk(flatData);
            var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);

            switch (forkType)
            {
                case ForkType.Deneb:
                    var denebLightClientBootstrap = DenebLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    syncProtocol.InitialiseStoreFromDenebBootstrap(syncProtocol.Options.TrustedBlockRoot, denebLightClientBootstrap);
                    syncProtocol.SetActiveFork(forkType);
                    break;
                case ForkType.Capella:
                    var capellaLightClientBootstrap = CapellaLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    syncProtocol.InitialiseStoreFromCapellaBootstrap(syncProtocol.Options.TrustedBlockRoot, capellaLightClientBootstrap);
                    syncProtocol.SetActiveFork(forkType);
                    break;
                case ForkType.Bellatrix:
                    var bellatrixLightClientBootstrap = AltairLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    syncProtocol.InitialiseStoreFromAltairBootstrap(syncProtocol.Options.TrustedBlockRoot, bellatrixLightClientBootstrap);
                    syncProtocol.SetActiveFork(forkType);
                    break;
                case ForkType.Altair:
                    var altairLightClientBootstrap = AltairLightClientBootstrap.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    syncProtocol.InitialiseStoreFromAltairBootstrap(syncProtocol.Options.TrustedBlockRoot, altairLightClientBootstrap);
                    syncProtocol.SetActiveFork(forkType);
                    break;
                case ForkType.Phase0:
                    _logger?.LogError("Received light client bootstrap response with unexpected fork type from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    await downChannel.CloseAsync();
                    break;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error occured when trying to handle light client bootstrap response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        _logger?.LogInformation("Received light client bootstrap request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
        
        var receivedData = new List<byte[]>();
        
        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }
        
        var flatData = receivedData.SelectMany(x => x).ToArray();
        var result = ReqRespHelpers.DecodeRequest(flatData);

        if (result == null)
        {
            _logger?.LogError("Failed to decode light client bootstrap request from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
            return;
        }
        
        var request = LightClientBootstrapRequest.Deserialize(result);
        _logger?.LogInformation("Received light client bootstrap request from {PeerId} with trustedBlockRoot={trustedBlockRoot}", context.RemotePeer.Address.Get<P2P>(), Convert.ToHexString(request.BlockRoot));

        var response = Array.Empty<byte>();
        var encodedResponse = ReqRespHelpers.EncodeResponse(response, ResponseCodes.ResourceUnavailable);
    }
}