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

public class LightClientOptimisticUpdateProtocol(ISyncProtocol syncProtocol, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<LightClientOptimisticUpdateProtocol>();
    public string Id => "/eth2/beacon_chain/req/light_client_optimistic_update/1/ssz_snappy";
    
    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
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
                _logger?.LogInformation("Failed to handle light client optimistic update response from {PeerId} due to reason {Reason}", context.RemotePeer.Address.Get<P2P>(), (ResponseCodes)flatData[0]);
                await downChannel.CloseAsync();
                return;
            }
            
            var result = ReqRespHelpers.DecodeResponseChunk(flatData);
            var forkType = Phase0Helpers.ComputeForkType(result.Item2, syncProtocol.Options);
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
        
            switch (forkType)
            {
                case ForkType.Deneb:
                    var denebLightClientOptimisticUpdate = DenebLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    DenebProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.DenebLightClientStore, denebLightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    _logger?.LogInformation("Processed light client optimistic update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Capella:
                    var capellaLightClientOptimisticUpdate = CapellaLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    CapellaProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.CapellaLightClientStore, capellaLightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    _logger?.LogInformation("Processed light client optimistic update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Bellatrix:
                    var bellatrixLightClientOptimisticUpdate = AltairLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    AltairProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.AltairLightClientStore, bellatrixLightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    _logger?.LogInformation("Processed light client optimistic update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Altair:
                    var altairLightClientOptimisticUpdate = AltairLightClientOptimisticUpdate.Deserialize(result.Item3, syncProtocol.Options.Preset);
                    AltairProcessors.ProcessLightClientOptimisticUpdate(syncProtocol.AltairLightClientStore, altairLightClientOptimisticUpdate, currentSlot, syncProtocol.Options, syncProtocol.Logger);
                    _logger?.LogInformation("Processed light client optimistic update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
                case ForkType.Phase0:
                    _logger?.LogError("Received light client optimistic update response with unexpected fork type from {PeerId}", context.RemotePeer.Address.Get<P2P>());
                    break;
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to receieve light client optimistic update from {PeerId}", context.RemotePeer.Address.Get<P2P>());
            await downChannel.CloseAsync();
        }
    }

    public Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {
        throw new NotImplementedException();
    }
}