using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairSyncCommittee : IEquatable<AltairSyncCommittee>
{
    [SszElement(0, "Vector[Vector[uint8,48], SYNC_COMMITTEE_SIZE]")]
    public byte[][] PubKeys { get; protected init; }
    
    [SszElement(1, "Vector[uint8,48]")]
    public byte[] AggregatePubKey { get; protected init; } 
    
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
            if (!PubKeys[i].SequenceEqual(other.PubKeys[i]))
            {
                return false;
            }
        }
        
        return AggregatePubKey.SequenceEqual(other.AggregatePubKey);
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
        var hash = new HashCode();
        if (AggregatePubKey != null)
        {
            foreach (var byteValue in AggregatePubKey)
            {
                hash.Add(byteValue);
            }
        }

        if (PubKeys != null)
        {
            foreach (var pubKey in PubKeys)
            {
                if (pubKey != null)
                {
                    foreach (var byteValue in pubKey)
                    {
                        hash.Add(byteValue);
                    }
                }
            }
        }

        return hash.ToHashCode();
    }
    
    public static AltairSyncCommittee CreateFrom(byte[][] pubKeys, byte[] aggregatePubKey)
    {
        if (pubKeys.Length != Constants.SyncCommitteeSize)
        {
            throw new ArgumentException("PubKeys length must be equal to SyncCommitteeSize");
        }
        
        var altairSyncCommittee = new AltairSyncCommittee
        {
            PubKeys = pubKeys,
            AggregatePubKey = aggregatePubKey
        };
        
        return altairSyncCommittee;
    }
    
    public static AltairSyncCommittee CreateDefault()
    {
        var pubKeys = new byte[Constants.SyncCommitteeSize][];
        
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            pubKeys[i] = new byte[48];
        }
        
        return CreateFrom(pubKeys, new byte[48]);
    }
    
    public static int BytesLength => Constants.SyncCommitteeSize * BlsPublicKey.Length + BlsPublicKey.Length;
    
    public static byte[] Serialize(AltairSyncCommittee altairSyncCommittee)
    {
        var container = SszContainer.GetContainer<AltairSyncCommittee>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(altairSyncCommittee)];
        
        container.Serialize(altairSyncCommittee, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairSyncCommittee Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairSyncCommittee>(data, SizePreset.MainnetPreset);
        return result.Item1;
    }
}