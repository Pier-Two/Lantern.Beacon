using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class Status : IEquatable<Status> 
{
    [SszElement(0, "Vector[uint8, 4]")] 
    public byte[] ForkDigest { get; init; }
    
    [SszElement(1, "Vector[uint8, 32]")] 
    public byte[] FinalizedRoot { get; init; }
    
    [SszElement(2, "uint64")]
    public ulong FinalizedEpoch { get; init; }
    
    [SszElement(3, "Vector[uint8, 32]")]
    public byte[] HeadRoot { get; init; }
    
    [SszElement(4, "uint64")]
    public ulong HeadSlot { get; init; }
    
    public bool Equals(Status? other)
    {
        return other != null && ForkDigest.SequenceEqual(other.ForkDigest) && FinalizedRoot.Equals(other.FinalizedRoot) && FinalizedEpoch.Equals(other.FinalizedEpoch) && HeadRoot.SequenceEqual(other.HeadRoot) && HeadSlot.Equals(other.HeadSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Status other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(ForkDigest, FinalizedRoot, FinalizedEpoch, HeadRoot, HeadSlot);
    }
    
    public static Status CreateFrom(byte[] forkDigest, byte[] finalizedRoot, ulong finalizedEpoch, byte[] headRoot, ulong headSlot)
    {
        return new Status
        {
            ForkDigest = forkDigest,
            FinalizedRoot = finalizedRoot,
            FinalizedEpoch = finalizedEpoch,
            HeadRoot = headRoot,
            HeadSlot = headSlot
        };
    }
    
    public static Status CreateDefault()
    {
        return new Status
        {
            ForkDigest = new byte[Constants.VersionLength],
            FinalizedRoot = new byte[Constants.RootLength],
            FinalizedEpoch = 0,
            HeadRoot = new byte[Constants.RootLength],
            HeadSlot = 0
        };
    }
    
    public static byte[] Serialize(Status status)
    {
        return SszContainer.Serialize(status);
    }
    
    public static Status Deserialize(byte[] data)
    {
        return SszContainer.Deserialize<Status>(data).Item1;
    }
}