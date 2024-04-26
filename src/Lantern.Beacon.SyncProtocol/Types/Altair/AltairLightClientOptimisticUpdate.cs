using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.SimpleSerialize;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientOptimisticUpdate(AltairLightClientHeader attestedHeader, 
    AltairSyncAggregate altairSyncAggregate,
    Slot signatureSlot) : IEquatable<AltairLightClientOptimisticUpdate>
{
    public AltairLightClientHeader AttestedHeader { get; init; } = attestedHeader;
    
    public AltairSyncAggregate AltairSyncAggregate { get; init; } = altairSyncAggregate;
    
    public Slot SignatureSlot { get; init; } = signatureSlot;
    
    public bool Equals(AltairLightClientOptimisticUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && AltairSyncAggregate.Equals(other.AltairSyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
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
        return HashCode.Combine(AttestedHeader, AltairSyncAggregate, SignatureSlot);
    }
    
    public static AltairLightClientOptimisticUpdate CreateDefault()
    {
        return new AltairLightClientOptimisticUpdate(AltairLightClientHeader.CreateDefault(), AltairSyncAggregate.CreateDefault(), Slot.Zero);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairSyncAggregate.BytesLength + sizeof(ulong);
}