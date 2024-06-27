namespace Lantern.Beacon.Sync.Config;

public static class MainnetConfig
{
    public const int MinGenesisTime = 1606824000;
    
    public const int GenesisDelay = 604800;
    
    public const int SecondsPerSlot = 12;
    
    public const int TimeToFirstByteTimeout = 5;
    
    public const int RespTimeout = 10;
    
    public const int AttestationSubnetCount = 64;
    
    public const int SyncCommitteeSubnetCount = 4;
    
    public const int GossipMaxSize = 10485760;
    
    public const int MaxRequestLightClientUpdates = 128;
    
    public const uint GenesisForkVersion = 0x00000000;
    
    public const uint AltairForkVersion = 0x01000000;
    
    public const uint AltairForkEpoch = 74240;
    
    public const uint BellatrixForkVersion = 0x02000000;
    
    public const uint BellatrixForkEpoch = 144896;
    
    public const uint CapellaForkVersion = 0x03000000;
    
    public const uint CapellaForkEpoch = 194048;
    
    public const uint DenebForkVersion = 0x04000000;
    
    public const uint DenebForkEpoch = 269568;
    
    public const uint ElectraForkVersion = 0x05000000;
}