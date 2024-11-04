using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientOptimisticUpdate : IEquatable<DenebLightClientOptimisticUpdate>
{
    [JsonPropertyName("attested_header")] 
    public DenebLightClientHeader AttestedHeaderJson => AttestedHeader;

    [JsonPropertyName("sync_aggregate")]
    public AltairSyncAggregate SyncAggregateJson => SyncAggregate;
    
    [JsonPropertyName("signature_slot")]
    public string SignatureSlotString => SignatureSlot.ToString();
    
    [JsonIgnore] 
    [SszElement(0, "Container")]
    public DenebLightClientHeader AttestedHeader { get; protected init; } 
    
    [JsonIgnore] 
    [SszElement(1, "Container")]
    public AltairSyncAggregate SyncAggregate { get; protected init; } 
    
    [JsonIgnore] 
    [SszElement(2, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(DenebLightClientOptimisticUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is DenebLightClientOptimisticUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, SyncAggregate, SignatureSlot);
    }
    
    public static DenebLightClientOptimisticUpdate CreateFromCapella(CapellaLightClientOptimisticUpdate pre)
    {
        return new DenebLightClientOptimisticUpdate
        {
            AttestedHeader = DenebLightClientHeader.CreateFromCapella(pre.AttestedHeader),
            SyncAggregate = pre.SyncAggregate,
            SignatureSlot = pre.SignatureSlot
        };
    }
    
    public static DenebLightClientOptimisticUpdate CreateFrom(
        DenebLightClientHeader denebLightClientHeader, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        return new DenebLightClientOptimisticUpdate
        {
            AttestedHeader = denebLightClientHeader,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static DenebLightClientOptimisticUpdate CreateDefault()
    {
        return CreateFrom(DenebLightClientHeader.CreateDefault(), AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(DenebLightClientOptimisticUpdate denebLightClientOptimisticUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientOptimisticUpdate>(preset);
        var bytes = new byte[container.Length(denebLightClientOptimisticUpdate)];
        
        container.Serialize(denebLightClientOptimisticUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static DenebLightClientOptimisticUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<DenebLightClientOptimisticUpdate>(data,preset);
        return result.Item1;
    }
    
    public static byte[] GetHttpResponse(DenebLightClientOptimisticUpdate update, string accept, SizePreset preset)
    {
        if (accept.Contains("application/octet-stream"))
        {
            return Serialize(update, preset);
        }

        var response = new
        {
            version = ForkType.Deneb.ToString().ToLower(),
            data = update
        };
        
        return ConvertToJsonBytes(response);
    }
    
    private static byte[] ConvertToJsonBytes(object obj)
    {
        var jsonString = JsonSerializer.Serialize(obj);
        return Encoding.UTF8.GetBytes(jsonString);
    }
}