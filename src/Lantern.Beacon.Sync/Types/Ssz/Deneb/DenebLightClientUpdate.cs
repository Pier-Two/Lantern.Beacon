using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientUpdate : IEquatable<DenebLightClientUpdate>
{
    [SszElement(0, "Container")]
    public DenebLightClientHeader AttestedHeader { get; internal set; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] NextSyncCommitteeBranch { get; internal set; } 
    
    [SszElement(3, "Container")]
    public DenebLightClientHeader FinalizedHeader { get; internal set; } 
    
    [SszElement(4, "Vector[Vector[uint8, 32], 6]")]
    public byte[][] FinalityBranch { get; internal set; } 
    
    [SszElement(5, "Container")]
    public AltairSyncAggregate SyncAggregate { get; internal set; } 
    
    [SszElement(6, "uint64")]
    public ulong SignatureSlot { get; internal set; } 
    
    public bool Equals(DenebLightClientUpdate? other)
    {
        if (other == null) return false;
        
        if (!AttestedHeader.Equals(other.AttestedHeader) ||
            !NextSyncCommittee.Equals(other.NextSyncCommittee) ||
            !FinalizedHeader.Equals(other.FinalizedHeader) ||
            !SyncAggregate.Equals(other.SyncAggregate) ||
            !SignatureSlot.Equals(other.SignatureSlot))
        {
            return false;
        }
        
        if (NextSyncCommitteeBranch.Length != other.NextSyncCommitteeBranch.Length)
        {
            return false;
        }

        for (var i = 0; i < NextSyncCommitteeBranch.Length; i++)
        {
            if (!NextSyncCommitteeBranch[i].SequenceEqual(other.NextSyncCommitteeBranch[i]))
            {
                return false;
            }
        }
        
        if (FinalityBranch.Length != other.FinalityBranch.Length)
        {
            return false;
        }

        for (var i = 0; i < FinalityBranch.Length; i++)
        {
            if (!FinalityBranch[i].SequenceEqual(other.FinalityBranch[i]))
            {
                return false;
            }
        }

        return true;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is DenebLightClientUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, NextSyncCommittee, NextSyncCommitteeBranch, FinalizedHeader, FinalityBranch, SyncAggregate, SignatureSlot);
    }
    
    public static DenebLightClientUpdate CreateFromCapella(CapellaLightClientUpdate pre)
    {
        return new DenebLightClientUpdate
        {
            AttestedHeader = DenebLightClientHeader.CreateFromCapella(pre.AttestedHeader),
            NextSyncCommittee = pre.NextSyncCommittee,
            NextSyncCommitteeBranch = pre.NextSyncCommitteeBranch,
            FinalizedHeader = DenebLightClientHeader.CreateFromCapella(pre.FinalizedHeader),
            FinalityBranch = pre.FinalityBranch,
            SyncAggregate = pre.SyncAggregate,
            SignatureSlot = pre.SignatureSlot
        };
    }
    
    public static DenebLightClientUpdate CreateFrom(
        DenebLightClientHeader attestedHeader, 
        AltairSyncCommittee nextAltairSyncCommittee, 
        byte[][] nextSyncCommitteeBranch, 
        DenebLightClientHeader finalizedHeader, 
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
        
        return new DenebLightClientUpdate
        {
            AttestedHeader = attestedHeader,
            NextSyncCommittee = nextAltairSyncCommittee,
            NextSyncCommitteeBranch = nextSyncCommitteeBranch,
            FinalizedHeader = finalizedHeader,
            FinalityBranch = finalityBranch,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static DenebLightClientUpdate CreateDefault()
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
        
        return CreateFrom(DenebLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), nextSyncCommitteeBranch, DenebLightClientHeader.CreateDefault(), finalityBranch, AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(DenebLightClientUpdate denebLightClientUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientUpdate>(preset);
        var bytes = new byte[container.Length(denebLightClientUpdate)];
        
        container.Serialize(denebLightClientUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static DenebLightClientUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<DenebLightClientUpdate>(data, preset);
        return result.Item1;
    }
}