using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Altair;

public class AltairLightClientUpdate : IEquatable<AltairLightClientUpdate>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader AttestedHeader { get; internal set; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] NextSyncCommitteeBranch { get; internal set; } 
    
    [SszElement(3, "Container")]
    public AltairLightClientHeader FinalizedHeader { get; internal set; } 
    
    [SszElement(4, "Vector[Vector[uint8, 32], 6]")]
    public byte[][] FinalityBranch { get; internal set; } 
    
    [SszElement(5, "Container")]
    public AltairSyncAggregate SyncAggregate { get; internal set; } 
    
    [SszElement(6, "uint64")]
    public ulong SignatureSlot { get; internal set; } 
    
    public bool Equals(AltairLightClientUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && NextSyncCommittee.Equals(other.NextSyncCommittee) && NextSyncCommitteeBranch.SequenceEqual(other.NextSyncCommitteeBranch) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
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
        return HashCode.Combine(AttestedHeader, NextSyncCommittee, NextSyncCommitteeBranch, FinalizedHeader, FinalityBranch, SyncAggregate, SignatureSlot);
    }
    
    public static AltairLightClientUpdate CreateFrom(
        AltairLightClientHeader altairLightClientHeader, 
        AltairSyncCommittee nextAltairSyncCommittee, 
        byte[][] nextSyncCommitteeBranch, 
        AltairLightClientHeader finalizedHeader, 
        byte[][] finalityBranch, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        if (nextSyncCommitteeBranch.Length != Constants.NextSyncCommitteeBranchDepth)
        {
            throw new ArgumentException($"Next sync committee branch length must be {Constants.NextSyncCommitteeBranchDepth}");
        }
        
        if (finalityBranch.Length != Constants.FinalityBranchDepth)
        {
            throw new ArgumentException($"Finalized branch length must be {Constants.FinalityBranchDepth}");
        }
        
        return new AltairLightClientUpdate
        {
            AttestedHeader = altairLightClientHeader,
            NextSyncCommittee = nextAltairSyncCommittee,
            NextSyncCommitteeBranch = nextSyncCommitteeBranch,
            FinalizedHeader = finalizedHeader,
            FinalityBranch = finalityBranch,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static AltairLightClientUpdate CreateDefault()
    {
        var nextSyncCommitteeBranch = new byte[Constants.NextSyncCommitteeBranchDepth][];
        var finalityBranch = new byte[Constants.FinalityBranchDepth][];

        for (var i = 0; i < nextSyncCommitteeBranch.Length; i++)
        {
            nextSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        for (var i = 0; i < finalityBranch.Length; i++)
        {
            finalityBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), nextSyncCommitteeBranch, AltairLightClientHeader.CreateDefault(), finalityBranch, AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(AltairLightClientUpdate altairLightClientUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairLightClientUpdate>(preset);
        var bytes = new byte[container.Length(altairLightClientUpdate)];
        
        container.Serialize(altairLightClientUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<AltairLightClientUpdate>(data, preset);
        return result.Item1;
    }
}