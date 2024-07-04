using Lantern.Beacon.Sync.Types.Ssz.Altair;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Capella;

public class CapellaLightClientOptimisticUpdate : IEquatable<CapellaLightClientOptimisticUpdate>
{
    [SszElement(0, "Container")]
    public CapellaLightClientHeader AttestedHeader { get; protected init; } 
    
    [SszElement(1, "Container")]
    public AltairSyncAggregate SyncAggregate { get; protected init; } 
    
    [SszElement(2, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(CapellaLightClientOptimisticUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is CapellaLightClientOptimisticUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, SyncAggregate, SignatureSlot);
    }
    
    public static CapellaLightClientOptimisticUpdate CreateFromAltair(AltairLightClientUpdate pre)
    {
        return new CapellaLightClientOptimisticUpdate
        {
            AttestedHeader = CapellaLightClientHeader.CreateFromAltair(pre.AttestedHeader),
            SyncAggregate = pre.SyncAggregate,
            SignatureSlot = pre.SignatureSlot
        };
    }
    
    public static CapellaLightClientOptimisticUpdate CreateFrom(
        CapellaLightClientHeader altairLightClientHeader, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        return new CapellaLightClientOptimisticUpdate
        {
            AttestedHeader = altairLightClientHeader,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static CapellaLightClientOptimisticUpdate CreateDefault()
    {
        return CreateFrom(CapellaLightClientHeader.CreateDefault(), AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(CapellaLightClientOptimisticUpdate capellaLightClientOptimisticUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<CapellaLightClientOptimisticUpdate>(preset);
        var bytes = new byte[container.Length(capellaLightClientOptimisticUpdate)];
        
        container.Serialize(capellaLightClientOptimisticUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static CapellaLightClientOptimisticUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<CapellaLightClientOptimisticUpdate>(data,preset);
        return result.Item1;
    }
}