using SszSharp;

namespace Lantern.Beacon.Sync.Types.Phase0;

public class BeaconBlocksByRange : IEquatable<BeaconBlocksByRange>
{
    [SszElement(0, "uint64")]
    public ulong StartSlot { get; private init; }
    
    [SszElement(1, "uint64")] 
    public ulong Count { get; private init; }

    [SszElement(2, "uint64")] 
    public ulong Step { get; private init; }
    
    public bool Equals(BeaconBlocksByRange? other)
    {
        return other != null && StartSlot.Equals(other.StartSlot) && Count.Equals(other.Count) && Step.Equals(other.Step);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is BeaconBlocksByRange other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(StartSlot, Count, Step);
    }
    
    public static BeaconBlocksByRange CreateFrom(ulong startSlot, ulong count)
    {
        return new BeaconBlocksByRange
        {
            StartSlot = startSlot,
            Count = count,
            Step = 1
        };
    }
    
    public static byte[] Serialize(BeaconBlocksByRange beaconBlocksByRange)
    {
        return SszContainer.Serialize(beaconBlocksByRange);
    }
    
    public static BeaconBlocksByRange Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<BeaconBlocksByRange>(data);
        return result.Item1;
    }
}