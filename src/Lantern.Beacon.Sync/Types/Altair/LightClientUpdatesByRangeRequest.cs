using SszSharp;

namespace Lantern.Beacon.Sync.Types.Altair;

public class LightClientUpdatesByRangeRequest : IEquatable<LightClientUpdatesByRangeRequest>
{
    [SszElement(0, "uint64")]
    public ulong StartPeriod { get; init; }
    
    [SszElement(1, "uint64")]
    public ulong Count { get; init; }
    
    public bool Equals(LightClientUpdatesByRangeRequest? other)
    {
        return other != null && StartPeriod.Equals(other.StartPeriod) && Count.Equals(other.Count);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientUpdatesByRangeRequest other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(StartPeriod, Count);
    }
    
    public static LightClientUpdatesByRangeRequest CreateFrom(ulong startPeriod, ulong count)
    {
        return new LightClientUpdatesByRangeRequest
        {
            StartPeriod = startPeriod,
            Count = count
        };
    }
 
    public static byte[] Serialize(LightClientUpdatesByRangeRequest lightClientUpdatesByRangeRequest)
    {
        return SszContainer.Serialize(lightClientUpdatesByRangeRequest);
    }
    
    public static LightClientUpdatesByRangeRequest Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<LightClientUpdatesByRangeRequest>(data);
        return result.Item1;
    }
}