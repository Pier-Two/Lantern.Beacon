using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class LightClientOptimisticUpdate(LightClientHeader attestedHeader, 
    SyncAggregate syncAggregate,
    Slot signatureSlot) : IEquatable<LightClientOptimisticUpdate>
{
    public LightClientHeader AttestedHeader { get; init; } = attestedHeader;
    
    public SyncAggregate SyncAggregate { get; init; } = syncAggregate;
    
    public Slot SignatureSlot { get; init; } = signatureSlot;
    
    public bool Equals(LightClientOptimisticUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientOptimisticUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, SyncAggregate, SignatureSlot);
    }
    
    public static LightClientOptimisticUpdate CreateDefault()
    {
        return new LightClientOptimisticUpdate(LightClientHeader.CreateDefault(), SyncAggregate.CreateDefault(), Slot.Zero);
    }
    
    public static int BytesLength => LightClientHeader.BytesLength + SyncAggregate.BytesLength + sizeof(ulong);
    
    public static class Serializer
    {
        public static byte[] Serialize(LightClientOptimisticUpdate optimisticUpdate)
        {
            var result = new byte[BytesLength];
            var attestedHeaderBytes = LightClientHeader.Serializer.Serialize(optimisticUpdate.AttestedHeader);
            var syncAggregateBytes = SyncAggregate.Serializer.Serialize(optimisticUpdate.SyncAggregate);
            
            Array.Copy(attestedHeaderBytes, 0, result, 0, LightClientHeader.BytesLength);
            Array.Copy(syncAggregateBytes, 0, result, LightClientHeader.BytesLength, SyncAggregate.BytesLength);
            
            Ssz.Encode(result.AsSpan(LightClientHeader.BytesLength + SyncAggregate.BytesLength, sizeof(ulong)), (ulong)optimisticUpdate.SignatureSlot);
            
            return result;
        }
        
        public static LightClientOptimisticUpdate Deserialize(byte[] bytes)
        {
            var attestedHeader = LightClientHeader.Serializer.Deserialize(bytes.AsSpan(0, LightClientHeader.BytesLength));
            var syncAggregate = SyncAggregate.Serializer.Deserialize(bytes.AsSpan(LightClientHeader.BytesLength, SyncAggregate.BytesLength));
            var signatureSlot = Ssz.DecodeULong(bytes.AsSpan(LightClientHeader.BytesLength + SyncAggregate.BytesLength, sizeof(ulong)));
            
            return new LightClientOptimisticUpdate(attestedHeader, syncAggregate, new Slot(signatureSlot));
        }
    }
}