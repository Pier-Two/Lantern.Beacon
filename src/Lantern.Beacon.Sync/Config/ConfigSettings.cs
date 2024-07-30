namespace Lantern.Beacon.Sync.Config;

public class ConfigSettings
{
    public int MinGenesisTime { get; set; }
    
    public int GenesisDelay { get; set; }
    
    public int SecondsPerSlot { get; set; }
    
    public int TimeToFirstByteTimeout { get; set; }
    
    public int RespTimeout { get; set; }
    
    public int AttestationSubnetCount { get; set; }
    
    public int SyncCommitteeSubnetCount { get; set; }
    
    public int GossipMaxSize { get; set; }
    
    public int MaxRequestLightClientUpdates { get; set; }
    
    public uint GenesisForkVersion { get; set; }
    
    public uint AltairForkVersion { get; set; }

    public uint AltairForkEpoch { get; set; }
    
    public uint BellatrixForkVersion { get; set; }
    
    public uint BellatrixForkEpoch { get; set; }
    
    public uint CapellaForkVersion { get; set; }
    
    public uint CapellaForkEpoch { get; set; }
    
    public uint DenebForkVersion { get; set; }
    
    public uint DenebForkEpoch { get; set; }
    
    public uint ElectraForkVersion { get; set; }
}