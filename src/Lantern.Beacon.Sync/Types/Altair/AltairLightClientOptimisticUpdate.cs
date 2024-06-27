using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Altair;

public class AltairLightClientOptimisticUpdate : IEquatable<AltairLightClientOptimisticUpdate>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader AttestedHeader { get; protected init; } 
    
    [SszElement(1, "Container")]
    public AltairSyncAggregate SyncAggregate { get; protected init; } 
    
    [SszElement(2, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(AltairLightClientOptimisticUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientOptimisticUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, SyncAggregate, SignatureSlot);
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairLightClientOptimisticUpdate>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static AltairLightClientOptimisticUpdate CreateFrom(
        AltairLightClientHeader altairLightClientHeader, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        return new AltairLightClientOptimisticUpdate
        {
            AttestedHeader = altairLightClientHeader,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static AltairLightClientOptimisticUpdate CreateDefault()
    {
        return CreateFrom(AltairLightClientHeader.CreateDefault(), AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(AltairLightClientOptimisticUpdate altairLightClientOptimisticUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairLightClientOptimisticUpdate>(preset);
        var bytes = new byte[container.Length(altairLightClientOptimisticUpdate)];
        
        container.Serialize(altairLightClientOptimisticUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientOptimisticUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<AltairLightClientOptimisticUpdate>(data,preset);
        return result.Item1;
    }
}