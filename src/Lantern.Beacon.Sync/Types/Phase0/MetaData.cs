using Lantern.Beacon.Sync.Config;

namespace Lantern.Beacon;

public class MetaData(ulong seqNumber, List<bool> attnets) : IEquatable<MetaData>
{
    public ulong SeqNumber { get; protected init; } = seqNumber;
    
    public List<bool> Attnets { get; protected init; } = attnets;
    
    public bool Equals(MetaData? other)
    {
        return other != null && SeqNumber == other.SeqNumber && Attnets.SequenceEqual(other.Attnets);
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
        return new MetaData(0, Enumerable.Repeat(false, Config.AttestationSubnetCount).ToList());
    }
}