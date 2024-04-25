using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class LightClientHeader(BeaconBlockHeader beacon) : IEquatable<LightClientHeader>
{
    public BeaconBlockHeader Beacon { get; init; } = beacon;
    
    public bool Equals(LightClientHeader? other)
    {
        return other != null && Beacon.Equals(other.Beacon);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientHeader other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return Beacon.GetHashCode();
    }
    
    public static LightClientHeader CreateDefault()
    {
        return new LightClientHeader(BeaconBlockHeader.CreateDefault());
    }

    public static int BytesLength => BeaconBlockHeader.BytesLength;
    
    public static class Serializer
    {
        public static byte[] Serialize(LightClientHeader header)
        {
            return BeaconBlockHeader.Serializer.Serialize(header.Beacon);
        }
        
        public static LightClientHeader Deserialize(byte[] bytes)
        {
            return new LightClientHeader(BeaconBlockHeader.Serializer.Deserialize(bytes));
        }
        
        public static LightClientHeader Deserialize(Span<byte> bytes)
        {
            return new LightClientHeader(BeaconBlockHeader.Serializer.Deserialize(bytes));
        }
    }
}