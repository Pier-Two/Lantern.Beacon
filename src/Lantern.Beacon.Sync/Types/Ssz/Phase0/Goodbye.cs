using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class Goodbye : IEquatable<Goodbye>
{
    [SszElement(0, "uint64")]
    public ulong Reason { get; init; }
    
    public bool Equals(Goodbye? other)
    {
        return other != null && Reason.Equals(other.Reason);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Goodbye other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Reason);
    }
    
    public static Goodbye CreateFrom(ulong reason)
    {
        return new Goodbye
        {
            Reason = reason
        };
    }
    
    public static Goodbye CreateDefault()
    {
        return new Goodbye
        {
            Reason = 0
        };
    }
    
    public static byte[] Serialize(Goodbye goodbye)
    {
        return SszContainer.Serialize(goodbye);
    }
    
    public static Goodbye Deserialize(byte[] data)
    {
        return SszContainer.Deserialize<Goodbye>(data).Item1;
    }
}