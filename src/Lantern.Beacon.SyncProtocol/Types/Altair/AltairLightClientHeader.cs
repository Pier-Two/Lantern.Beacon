using Lantern.Beacon.SyncProtocol.Types.Phase0;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientHeader(Phase0BeaconBlockHeader beacon) : IEquatable<AltairLightClientHeader>
{
    public Phase0BeaconBlockHeader Beacon { get; init; } = beacon;
    
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
    
    public static AltairLightClientHeader CreateDefault()
    {
        return new AltairLightClientHeader(Phase0BeaconBlockHeader.CreateDefault());
    }

    public static int BytesLength => Phase0BeaconBlockHeader.BytesLength;
}