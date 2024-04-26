using System.Collections;
using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairSyncAggregate(BitArray syncCommitteeBits, BlsSignature syncCommitteeSignature) : IEquatable<AltairSyncAggregate>
{
    public BitArray SyncCommitteeBits { get; init; } = syncCommitteeBits;
    
    public BlsSignature SyncCommitteeSignature { get; init; } = syncCommitteeSignature;
    
    public bool Equals(AltairSyncAggregate? other)
    {
        return other != null && SyncCommitteeBits.Equals(other.SyncCommitteeBits) && SyncCommitteeSignature.Equals(other.SyncCommitteeSignature);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairSyncAggregate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(SyncCommitteeBits, SyncCommitteeSignature);
    }
    
    public static AltairSyncAggregate CreateDefault()
    {
        return new AltairSyncAggregate(new BitArray(Constants.SyncCommitteeSize), new BlsSignature());
    }
    
    public static int BytesLength => Constants.SyncCommitteeSize + BlsSignature.Length;
}