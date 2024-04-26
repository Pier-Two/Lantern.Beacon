using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.SimpleSerialize;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientFinalityUpdate(AltairLightClientHeader attestedHeader,
    AltairLightClientHeader finalizedHeader,
    Bytes32[] finalityBranch, 
    AltairSyncAggregate altairSyncAggregate,
    Slot signatureSlot) : IEquatable<AltairLightClientFinalityUpdate>
{
    public AltairLightClientHeader AttestedHeader { get; init; } = attestedHeader;
    
    public AltairLightClientHeader FinalizedHeader { get; init; } = finalizedHeader;
    
    public Bytes32[] FinalityBranch { get; init; } = finalityBranch;
    
    public AltairSyncAggregate AltairSyncAggregate { get; init; } = altairSyncAggregate;
    
    public Slot SignatureSlot { get; init; } = signatureSlot;
    
    public bool Equals(AltairLightClientFinalityUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && AltairSyncAggregate.Equals(other.AltairSyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientFinalityUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, FinalizedHeader, FinalityBranch, AltairSyncAggregate, SignatureSlot);
    }
    
    public static AltairLightClientFinalityUpdate CreateDefault()
    {
        return new AltairLightClientFinalityUpdate(AltairLightClientHeader.CreateDefault(), AltairLightClientHeader.CreateDefault(), new Bytes32[Constants.FinalizedRootGIndex], AltairSyncAggregate.CreateDefault(), Slot.Zero);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairLightClientHeader.BytesLength + Constants.FinalityBranchDepth * Bytes32.Length + AltairSyncAggregate.BytesLength + sizeof(ulong);
}