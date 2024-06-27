namespace Lantern.Beacon.Sync.Config;

public static class MinimalConfig
{
    public const int MinGenesisTime = 1578009600;
    
    public const int GenesisDelay = 300;
    
    public const int SecondsPerSlot = 6;
    
    public const int TimeToFirstByteTimeout = 5;
    
    public const int RespTimeout = 10;
    
    public const int AttestationSubnetCount = 64;
    
    public const int SyncCommitteeSubnetCount = 4;
    
    public const int GossipMaxSize = 10485760;
    
    public const int MaxRequestLightClientUpdates = 128;
    
    public const uint GenesisForkVersion = 0x00000001;
    
    public const uint AltairForkVersion = 0x01000001;
    
    public const uint AltairForkEpoch = 1844674407;
    
    public const uint BellatrixForkVersion = 0x02000001;
    
    public const uint BellatrixForkEpoch = 1844674407;
    
    public const uint CapellaForkVersion = 0x03000001;
    
    public const uint CapellaForkEpoch = 1844674407;
    
    public const uint DenebForkVersion = 0x04000001;
    
    public const uint DenebForkEpoch = 1844674407;
    
    public const uint ElectraForkVersion = 0x05000001;
}