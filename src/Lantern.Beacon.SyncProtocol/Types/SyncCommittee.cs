using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class SyncCommittee(BlsPublicKey[] pubKeys, BlsPublicKey aggregatePubKey) : IEquatable<SyncCommittee>
{
    public BlsPublicKey[] PubKeys { get; init; } = pubKeys;
    
    public BlsPublicKey AggregatePubKey { get; init; } = aggregatePubKey;
    
    public bool Equals(SyncCommittee? other)
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
        if (obj is SyncCommittee other)
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
    
    public static SyncCommittee CreateDefault()
    {
        return new SyncCommittee(new BlsPublicKey[Constants.SyncCommitteeSize], new BlsPublicKey());
    }
    
    public static int BytesLength => Constants.SyncCommitteeSize * BlsPublicKey.Length + BlsPublicKey.Length;
    
    public static class Serializer
    {
        public static byte[] Serialize(SyncCommittee syncCommittee)
        {
            var result = new byte[BytesLength];
            var offset = 0;
            
            foreach (var pubKey in syncCommittee.PubKeys)
            {
                Ssz.Encode(result.AsSpan(offset, BlsPublicKey.Length), pubKey.AsSpan());
                offset += BlsPublicKey.Length;
            }
            
            Ssz.Encode(result.AsSpan(offset, BlsPublicKey.Length), syncCommittee.AggregatePubKey.AsSpan());

            return result;
        }
        
        public static SyncCommittee Deserialize(byte[] bytes)
        {
            var offset = 0;
            var pubKeys = new BlsPublicKey[Constants.SyncCommitteeSize];
            
            for (var i = 0; i < Constants.SyncCommitteeSize; i++)
            {
                pubKeys[i] = new BlsPublicKey(bytes.AsSpan(offset, BlsPublicKey.Length));
                offset += BlsPublicKey.Length;
            }
            
            var aggregatePubKey = new BlsPublicKey(bytes.AsSpan(offset, BlsPublicKey.Length));
            
            return new SyncCommittee(pubKeys, aggregatePubKey);
        }
        
        public static SyncCommittee Deserialize(Span<byte> bytes)
        {
            var offset = 0;
            var pubKeys = new BlsPublicKey[Constants.SyncCommitteeSize];
            
            for (var i = 0; i < Constants.SyncCommitteeSize; i++)
            {
                pubKeys[i] = new BlsPublicKey(bytes.Slice(offset, BlsPublicKey.Length));
                offset += BlsPublicKey.Length;
            }
            
            var aggregatePubKey = new BlsPublicKey(bytes.Slice(offset, BlsPublicKey.Length));
            
            return new SyncCommittee(pubKeys, aggregatePubKey);
        }
    }
}