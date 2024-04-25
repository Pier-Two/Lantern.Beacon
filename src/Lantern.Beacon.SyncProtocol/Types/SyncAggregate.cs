using System.Collections;
using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class SyncAggregate(BitArray syncCommitteeBits, BlsSignature syncCommitteeSignature) : IEquatable<SyncAggregate>
{
    public BitArray SyncCommitteeBits { get; init; } = syncCommitteeBits;
    
    public BlsSignature SyncCommitteeSignature { get; init; } = syncCommitteeSignature;
    
    public bool Equals(SyncAggregate? other)
    {
        return other != null && SyncCommitteeBits.Equals(other.SyncCommitteeBits) && SyncCommitteeSignature.Equals(other.SyncCommitteeSignature);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is SyncAggregate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(SyncCommitteeBits, SyncCommitteeSignature);
    }
    
    public static SyncAggregate CreateDefault()
    {
        return new SyncAggregate(new BitArray(Constants.SyncCommitteeSize), new BlsSignature());
    }
    
    public static int BytesLength => Constants.SyncCommitteeSize + BlsSignature.Length;
    
    public static class Serializer
    {
        public static byte[] Serialize(SyncAggregate syncAggregate)
        {
            var result = new byte[BytesLength];
            
            Ssz.EncodeVector(result.AsSpan(0, Constants.SyncCommitteeSize), syncAggregate.SyncCommitteeBits);
            Ssz.Encode(result.AsSpan(Constants.SyncCommitteeSize, BlsSignature.Length), (ReadOnlySpan<byte>)syncAggregate.SyncCommitteeSignature);
            
            return result;
        }
        
        public static SyncAggregate Deserialize(byte[] bytes)
        {
            var syncCommitteeBits = Ssz.DecodeBitvector(bytes.AsSpan(0, Constants.SyncCommitteeSize), Constants.SyncCommitteeSize);
            var syncCommitteeSignature = new BlsSignature(Ssz.DecodeBytes(bytes.AsSpan(Constants.SyncCommitteeSize, BlsSignature.Length)));
            
            return new SyncAggregate(syncCommitteeBits, syncCommitteeSignature);
        }
        
        public static SyncAggregate Deserialize(Span<byte> bytes)
        {
            var syncCommitteeBits = Ssz.DecodeBitvector(bytes[..Constants.SyncCommitteeSize], Constants.SyncCommitteeSize);
            var syncCommitteeSignature = new BlsSignature(Ssz.DecodeBytes(bytes.Slice(Constants.SyncCommitteeSize, BlsSignature.Length)));
            
            return new SyncAggregate(syncCommitteeBits, syncCommitteeSignature);
        }
    }
}