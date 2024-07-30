using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types;
using SszSharp;

namespace Lantern.Beacon.Sync;

public class SyncProtocolOptions
{
    public SizePreset Preset { get; set; }
    
    public ulong GenesisTime { get; set; }
    
    public byte[] GenesisValidatorsRoot { get; set; } 
    
    public byte[] TrustedBlockRoot { get; set; }
    
    public NetworkType Network { get; set; }
    
    public ConfigSettings? ConfigSettings { get; set; }
    
    public PresetSettings? PresetSettings { get; set; }
}