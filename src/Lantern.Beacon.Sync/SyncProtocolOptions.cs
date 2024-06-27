using SszSharp;

namespace Lantern.Beacon.Sync;

public class SyncProtocolOptions
{
    public SizePreset Preset { get; set; }
    
    public ulong GenesisTime { get; set; }
    
    public byte[] GenesisValidatorsRoot { get; set; } 
    
    public byte[] TrustedBlockRoot { get; set; }
}