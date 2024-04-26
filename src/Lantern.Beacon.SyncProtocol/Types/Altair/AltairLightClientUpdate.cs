using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.SimpleSerialize;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientUpdate(AltairLightClientHeader attestedHeader,
    AltairSyncCommittee nextAltairSyncCommittee,
    Bytes32[] nextSyncCommitteeBranch,
    AltairLightClientHeader finalizedHeader,
    Bytes32[] finalizedBranch,
    AltairSyncAggregate altairSyncAggregate,
    Slot signatureSlot) : IEquatable<AltairLightClientUpdate>
{
    public AltairLightClientHeader AttestedHeader { get; init; } = attestedHeader;
    
    public AltairSyncCommittee NextAltairSyncCommittee { get; init; } = nextAltairSyncCommittee;
    
    public Bytes32[] NextSyncCommitteeBranch { get; init; } = nextSyncCommitteeBranch;
    
    public AltairLightClientHeader FinalizedHeader { get; init; } = finalizedHeader;
    
    public Bytes32[] FinalizedBranch { get; init; } = finalizedBranch;
    
    public AltairSyncAggregate AltairSyncAggregate { get; init; } = altairSyncAggregate;
    
    public Slot SignatureSlot { get; init; } = signatureSlot;
    
    public bool Equals(AltairLightClientUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && NextAltairSyncCommittee.Equals(other.NextAltairSyncCommittee) && NextSyncCommitteeBranch.SequenceEqual(other.NextSyncCommitteeBranch) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalizedBranch.SequenceEqual(other.FinalizedBranch) && AltairSyncAggregate.Equals(other.AltairSyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, NextAltairSyncCommittee, NextSyncCommitteeBranch, FinalizedHeader, FinalizedBranch, AltairSyncAggregate, SignatureSlot);
    }
    
    public static AltairLightClientUpdate CreateDefault()
    {
        return new AltairLightClientUpdate(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), new Bytes32[Constants.NextSyncCommitteeGIndex], AltairLightClientHeader.CreateDefault(), new Bytes32[Constants.FinalizedRootGIndex], AltairSyncAggregate.CreateDefault(), Slot.Zero);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength + Constants.NextSyncCommitteeBranchDepth * Bytes32.Length + AltairLightClientHeader.BytesLength + Constants.FinalityBranchDepth * Bytes32.Length + AltairSyncAggregate.BytesLength + sizeof(ulong);
}