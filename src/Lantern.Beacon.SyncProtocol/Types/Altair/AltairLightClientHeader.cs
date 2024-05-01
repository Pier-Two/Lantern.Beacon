using Lantern.Beacon.SyncProtocol.Types.Phase0;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

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

    public static int BytesLength => Phase0BeaconBlockHeader.BytesLength;
    
    public static byte[] Serialize(AltairLightClientHeader altairLightClientHeader)
    {
        return SszContainer.Serialize(altairLightClientHeader, SizePreset.MainnetPreset);
    }
    
    public static AltairLightClientHeader Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairLightClientHeader>(data, SizePreset.DefaultPreset);
        return result.Item1;
    } 
}