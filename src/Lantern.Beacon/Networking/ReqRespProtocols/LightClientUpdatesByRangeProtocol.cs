using System.Buffers;
using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Deneb;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.ReqRespProtocols;

public class LightClientUpdatesByRangeProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientUpdatesByRangeProtocol>();
    public string Id => "/eth2/beacon_chain/req/light_client_updates_by_range/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        var request = LightClientUpdatesByRangeRequest.CreateFrom(1145,2);
        var sszData = LightClientUpdatesByRangeRequest.Serialize(request);
        var payload = ReqRespHelpers.EncodeRequest(sszData);
        var rawData = new ReadOnlySequence<byte>(payload);
        
        await downChannel.WriteAsync(rawData);
        var receivedData = new List<byte[]>();
        var responses = new List<byte[]>();
        var index = 0;

        await foreach (var readOnlySequence in downChannel.ReadAllAsync())
        {
            receivedData.Add(readOnlySequence.ToArray());
        }

        foreach (var data in receivedData)
        {
            if (data is [(byte)ResponseCodes.Success])
            {
                responses.Add(data);
                index++;
            }
            else
            {
                responses[index - 1] = responses[index - 1].Concat(data).ToArray();
            }
        }
        
        try
        {
            foreach (var responseChunk in responses)
            {
                var result = ReqRespHelpers.DecodeResponseChunk(responseChunk);
                var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);
                var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
                
                switch (forkType)
                {
                    case ForkType.Deneb:
                        var denebLightClientUpdate = DenebLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        _logger?.LogInformation("Processing light client update from {PeerId} for slot {Slot}", context.RemotePeer.Address.Get<P2P>(), denebLightClientUpdate.SignatureSlot);
                        DenebProcessors.ProcessLightClientUpdate(syncProtocol.DenebLightClientStore, denebLightClientUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                        break;
                    case ForkType.Capella:
                        var capellaLightClientUpdate = CapellaLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        CapellaProcessors.ProcessLightClientUpdate(syncProtocol.CapellaLightClientStore, capellaLightClientUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                        break;
                    case ForkType.Bellatrix:
                        var bellatrixLightClientUpdate = AltairLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        AltairProcessors.ProcessLightClientUpdate(syncProtocol.AltairLightClientStore, bellatrixLightClientUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                        break;
                    case ForkType.Altair:
                        var altairLightClientUpdate = AltairLightClientUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                        AltairProcessors.ProcessLightClientUpdate(syncProtocol.AltairLightClientStore, altairLightClientUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                        break;
                    case ForkType.Phase0:
                        _logger?.LogError("Received light client update response with unexpected fork type from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                        break;
                }
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to decode light client update response from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
    }

    public Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        throw new NotImplementedException();
    }
}