using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientOptimisticUpdate : IEquatable<DenebLightClientOptimisticUpdate>
{
    [SszElement(0, "Container")]
    public DenebLightClientHeader AttestedHeader { get; protected init; } 
    
    [SszElement(1, "Container")]
    public AltairSyncAggregate SyncAggregate { get; protected init; } 
    
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
}