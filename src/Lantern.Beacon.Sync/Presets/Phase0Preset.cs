using Lantern.Beacon.Sync.Presets.Minimal;
using Lantern.Beacon.Sync.Presets.Mainnet;

namespace Lantern.Beacon.Sync.Presets;

public static class Phase0Preset
{
    public static int SlotsPerEpoch { get; set; }
    
    public static void InitializeWithMainnet()
    {
        SlotsPerEpoch = Mainnet.Phase0PresetValues.SlotsPerEpoch;
    }
    
    public static void InitializeWithMinimal()
    {
        SlotsPerEpoch = Minimal.Phase0PresetValues.SlotsPerEpoch;
    }
}