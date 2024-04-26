using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairSyncCommittee(BlsPublicKey[] pubKeys, BlsPublicKey aggregatePubKey) : IEquatable<AltairSyncCommittee>
{
    public BlsPublicKey[] PubKeys { get; init; } = pubKeys;
    
    public BlsPublicKey AggregatePubKey { get; init; } = aggregatePubKey;
    
    public bool Equals(AltairSyncCommittee? other)
    {
        if (other == null)
        {
            return false;
        }
        
        if (PubKeys.Length != other.PubKeys.Length)
        {
            return false;
        }
        
        for (var i = 0; i < PubKeys.Length; i++)
        {
            if (!PubKeys[i].Equals(other.PubKeys[i]))
            {
                return false;
            }
        }
        
        return AggregatePubKey.Equals(other.AggregatePubKey);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairSyncCommittee other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        var hash = HashCode.Combine(AggregatePubKey);
        
        foreach (var pubKey in PubKeys)
        {
            hash = HashCode.Combine(hash, pubKey);
        }
        
        return hash;
    }
    
    public static AltairSyncCommittee CreateDefault()
    {
        return new AltairSyncCommittee(new BlsPublicKey[Constants.SyncCommitteeSize], new BlsPublicKey());
    }
    
    public static int BytesLength => Constants.SyncCommitteeSize * BlsPublicKey.Length + BlsPublicKey.Length;
    
    public static class Serializer
    {
   
        
   
    }
}