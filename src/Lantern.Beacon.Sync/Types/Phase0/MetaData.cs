using SszSharp;

namespace Lantern.Beacon.Sync.Types.Phase0;

public class MetaData : IEquatable<MetaData>
{
    [SszElement(0, "uint64")]
    public ulong SeqNumber { get; private set; } 
    
    [SszElement(1, "Bitvector[64]")]
    public List<bool> Attnets { get; private set; }
    
    [SszElement(2, "Bitvector[4]")]
    public List<bool> Syncnets { get; private set; }
    
    public bool Equals(MetaData? other)
    {
        return other != null && SeqNumber == other.SeqNumber && Attnets.SequenceEqual(other.Attnets) && Syncnets.SequenceEqual(other.Syncnets);
    }
    
    public void IncrementSeqNumber()
    {
        SeqNumber++;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is MetaData other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(SeqNumber);

        if (Attnets != null)
        {
            foreach (var bit in Attnets)
            {
                hash.Add(bit);
            }
        }

        return hash.ToHashCode();
    }
    
    public static MetaData CreateDefault()
    {
        return new MetaData
        {
            SeqNumber = 0,
            Attnets = Enumerable.Repeat(false, Config.Config.AttestationSubnetCount).ToList(),
            Syncnets = Enumerable.Repeat(false, Config.Config.SyncCommitteeSubnetCount).ToList()
        };
    }
    
    public static MetaData CreateFrom(ulong seqNumber, List<bool> attnets, List<bool> syncnets)
    {
        return new MetaData
        {
            SeqNumber = seqNumber,
            Attnets = attnets,
            Syncnets = syncnets
        };
    }
    
    public static byte[] Serialize(MetaData metaData)
    {
        return SszContainer.Serialize(metaData);
    }
    
    public static MetaData Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<MetaData>(data);
        return result.Item1;
    }
}