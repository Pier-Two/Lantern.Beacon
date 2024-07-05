using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class Ping : IEquatable<Ping>
{
    [SszElement(0, "uint64")]
    public ulong SeqNumber { get; init; }
    
    public bool Equals(Ping? other)
    {
        return other != null && SeqNumber.Equals(other.SeqNumber);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Ping other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(SeqNumber);
    }
    
    public static Ping CreateFrom(ulong seqNumber)
    {
        return new Ping
        {
            SeqNumber = seqNumber
        };
    }
    
    public static Ping CreateDefault()
    {
        return new Ping
        {
            SeqNumber = 0
        };
    }
    
    public static byte[] Serialize(Ping ping)
    {
        return SszContainer.Serialize(ping);
    }
    
    public static Ping Deserialize(byte[] data)
    {
        return SszContainer.Deserialize<Ping>(data).Item1;
    }
}