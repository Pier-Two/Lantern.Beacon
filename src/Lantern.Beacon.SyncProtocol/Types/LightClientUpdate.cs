using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class LightClientUpdate(LightClientHeader attestedHeader,
    SyncCommittee nextSyncCommittee,
    Bytes32[] nextSyncCommitteeBranch,
    LightClientHeader finalizedHeader,
    Bytes32[] finalizedBranch,
    SyncAggregate syncAggregate,
    Slot signatureSlot) : IEquatable<LightClientUpdate>
{
    public LightClientHeader AttestedHeader { get; init; } = attestedHeader;
    
    public SyncCommittee NextSyncCommittee { get; init; } = nextSyncCommittee;
    
    public Bytes32[] NextSyncCommitteeBranch { get; init; } = nextSyncCommitteeBranch;
    
    public LightClientHeader FinalizedHeader { get; init; } = finalizedHeader;
    
    public Bytes32[] FinalizedBranch { get; init; } = finalizedBranch;
    
    public SyncAggregate SyncAggregate { get; init; } = syncAggregate;
    
    public Slot SignatureSlot { get; init; } = signatureSlot;
    
    public bool Equals(LightClientUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && NextSyncCommittee.Equals(other.NextSyncCommittee) && NextSyncCommitteeBranch.SequenceEqual(other.NextSyncCommitteeBranch) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalizedBranch.SequenceEqual(other.FinalizedBranch) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, NextSyncCommittee, NextSyncCommitteeBranch, FinalizedHeader, FinalizedBranch, SyncAggregate, SignatureSlot);
    }
    
    public static LightClientUpdate CreateDefault()
    {
        return new LightClientUpdate(LightClientHeader.CreateDefault(), SyncCommittee.CreateDefault(), new Bytes32[Constants.NextSyncCommitteeGIndex], LightClientHeader.CreateDefault(), new Bytes32[Constants.FinalizedRootGIndex], SyncAggregate.CreateDefault(), Slot.Zero);
    }
    
    public static int BytesLength => LightClientHeader.BytesLength + SyncCommittee.BytesLength + Constants.NextSyncCommitteeBranchDepth * Bytes32.Length + LightClientHeader.BytesLength + Constants.FinalityBranchDepth * Bytes32.Length + SyncAggregate.BytesLength + sizeof(ulong);
    
    public static class Serializer
    {
        public static byte[] Serialize(LightClientUpdate update)
        {
            var result = new byte[BytesLength];
            var attestedHeaderBytes = LightClientHeader.Serializer.Serialize(update.AttestedHeader);
            var nextSyncCommitteeBytes = SyncCommittee.Serializer.Serialize(update.NextSyncCommittee);
            var finalizedHeaderBytes = LightClientHeader.Serializer.Serialize(update.FinalizedHeader);
            var syncAggregateBytes = SyncAggregate.Serializer.Serialize(update.SyncAggregate);
           
            Array.Copy(attestedHeaderBytes, 0, result, 0, LightClientHeader.BytesLength);
            Array.Copy(nextSyncCommitteeBytes, 0, result, LightClientHeader.BytesLength, SyncCommittee.BytesLength);
            
            var offset = LightClientHeader.BytesLength + SyncCommittee.BytesLength;
            
            foreach (var array in update.NextSyncCommitteeBranch)
            {
                Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
                offset += Bytes32.Length;
            }
            
            Array.Copy(finalizedHeaderBytes, 0, result, offset, LightClientHeader.BytesLength);
            
            offset += LightClientHeader.BytesLength;
            
            foreach (var array in update.FinalizedBranch)
            {
                Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
                offset += Bytes32.Length;
            }
            
            Array.Copy(syncAggregateBytes, 0, result, offset, SyncAggregate.BytesLength);
            
            Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), (ulong)update.SignatureSlot);
            
            return result;
        }
        
        public static LightClientUpdate Deserialize(byte[] bytes)
        {
            var attestedHeader = LightClientHeader.Serializer.Deserialize(bytes.AsSpan(0, LightClientHeader.BytesLength));
            var nextSyncCommittee = SyncCommittee.Serializer.Deserialize(bytes.AsSpan(LightClientHeader.BytesLength, SyncCommittee.BytesLength));
            var nextSyncCommitteeBranch = new Bytes32[Constants.NextSyncCommitteeBranchDepth];
            var offset = LightClientHeader.BytesLength + SyncCommittee.BytesLength;
            
            for (var i = 0; i < Constants.NextSyncCommitteeBranchDepth; i++)
            {
                nextSyncCommitteeBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
                offset += Bytes32.Length;
            }
            
            var finalizedHeader = LightClientHeader.Serializer.Deserialize(bytes.AsSpan(offset, LightClientHeader.BytesLength));
            var finalizedBranch = new Bytes32[Constants.FinalityBranchDepth];
            offset += LightClientHeader.BytesLength;
            
            for (var i = 0; i < Constants.FinalityBranchDepth; i++)
            {
                finalizedBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
                offset += Bytes32.Length;
            }
            
            var syncAggregate = SyncAggregate.Serializer.Deserialize(bytes.AsSpan(offset, SyncAggregate.BytesLength));
            offset += SyncAggregate.BytesLength;
            
            var signatureSlot = new Slot(Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong))));
            
            return new LightClientUpdate(attestedHeader, nextSyncCommittee, nextSyncCommitteeBranch, finalizedHeader, finalizedBranch, syncAggregate, signatureSlot);
        }
    }
}