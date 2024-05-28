using Lantern.Beacon.Sync.Types.Phase0;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Altair;

public class AltairLightClientHeader : IEquatable<AltairLightClientHeader>
{
    [SszElement(0, "Container")]
    public Phase0BeaconBlockHeader Beacon { get; protected init; }
    
    public bool Equals(AltairLightClientHeader? other)
    {
        return other != null && Beacon.Equals(other.Beacon);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientHeader other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return Beacon.GetHashCode();
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairLightClientHeader>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static AltairLightClientHeader CreateFrom(Phase0BeaconBlockHeader beaconBlockHeader)
    {
        return new AltairLightClientHeader
        {
            Beacon = beaconBlockHeader
        };
    }
    
    public static AltairLightClientHeader CreateDefault()
    {
        return CreateFrom(Phase0BeaconBlockHeader.CreateDefault());
    }
    
    public static byte[] Serialize(AltairLightClientHeader altairLightClientHeader, SizePreset preset)
    {
        return SszContainer.Serialize(altairLightClientHeader, preset);
    }
    
    public static AltairLightClientHeader Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<AltairLightClientHeader>(data, preset);
        return result.Item1;
    } 
}