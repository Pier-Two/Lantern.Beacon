using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class EnrForkId : IEquatable<EnrForkId>
{
    [SszElement(0, "Vector[uint8, 4]")]
    public byte[] ForkDigest { get; init; }
    
    [SszElement(1, "Vector[uint8, 4]")] 
    public byte[] NextForkVersion { get; init; }
    
    [SszElement(2, "uint64")]
    public ulong NextForkEpoch { get; init; }
    
    public bool Equals(EnrForkId? other)
    {
        return other != null && ForkDigest.SequenceEqual(other.ForkDigest) && NextForkVersion.SequenceEqual(other.NextForkVersion) && NextForkEpoch == other.NextForkEpoch;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is EnrForkId other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(ForkDigest, NextForkVersion, NextForkEpoch);
    }
    
    public static EnrForkId CreateFrom(byte[] forkDigest, byte[] nextForkVersion, ulong nextForkEpoch)
    {
        return new EnrForkId
        {
            ForkDigest = forkDigest,
            NextForkVersion = nextForkVersion,
            NextForkEpoch = nextForkEpoch
        };
    }
    
    public static byte[] Serialize(EnrForkId enrForkId, SizePreset preset)
    {
        return SszContainer.Serialize(enrForkId, preset);
    }
    
    public static EnrForkId Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<EnrForkId>(data, preset);
        return result.Item1;
    } 
}