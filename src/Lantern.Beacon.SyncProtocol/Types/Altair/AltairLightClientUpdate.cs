using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientUpdate : IEquatable<AltairLightClientUpdate>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader AttestedHeader { get; protected init; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee NextAltairSyncCommittee { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] NextSyncCommitteeBranch { get; protected init; } 
    
    [SszElement(3, "Container")]
    public AltairLightClientHeader FinalizedHeader { get; protected init; } 
    
    [SszElement(4, "Vector[Vector[uint8, 32], 6]")]
    public byte[][] FinalityBranch { get; protected init; } 
    
    [SszElement(5, "Container")]
    public AltairSyncAggregate AltairSyncAggregate { get; protected init; } 
    
    [SszElement(6, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(AltairLightClientUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && NextAltairSyncCommittee.Equals(other.NextAltairSyncCommittee) && NextSyncCommitteeBranch.SequenceEqual(other.NextSyncCommitteeBranch) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && AltairSyncAggregate.Equals(other.AltairSyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
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
        return HashCode.Combine(AttestedHeader, NextAltairSyncCommittee, NextSyncCommitteeBranch, FinalizedHeader, FinalityBranch, AltairSyncAggregate, SignatureSlot);
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
            NextAltairSyncCommittee = nextAltairSyncCommittee,
            NextSyncCommitteeBranch = nextSyncCommitteeBranch,
            FinalizedHeader = finalizedHeader,
            FinalityBranch = finalityBranch,
            AltairSyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static AltairLightClientUpdate CreateDefault()
    {
        return AltairLightClientUpdate.CreateFrom(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), new byte[Constants.NextSyncCommitteeBranchDepth][], AltairLightClientHeader.CreateDefault(), new byte[Constants.FinalityBranchDepth][], AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength + Constants.NextSyncCommitteeBranchDepth * Bytes32.Length + AltairLightClientHeader.BytesLength + Constants.FinalityBranchDepth * Bytes32.Length + AltairSyncAggregate.BytesLength + sizeof(ulong);
    
    public static byte[] Serialize(AltairLightClientUpdate altairLightClientUpdate)
    {
        var container = SszContainer.GetContainer<AltairLightClientUpdate>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(altairLightClientUpdate)];
        
        container.Serialize(altairLightClientUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientUpdate Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairLightClientUpdate>(data, SizePreset.MainnetPreset);
        return result.Item1;
    }
}