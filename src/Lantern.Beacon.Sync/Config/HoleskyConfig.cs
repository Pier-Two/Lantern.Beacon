namespace Lantern.Beacon.Sync.Config;

public class HoleskyConfig
{
    public const int MinGenesisTime = 1695902100;
    
    public const int GenesisDelay = 300;
    
    public const int SecondsPerSlot = 12;
    
    public const int TimeToFirstByteTimeout = 5;
    
    public const int RespTimeout = 10;
    
    public const int AttestationSubnetCount = 64;
    
    public const int SyncCommitteeSubnetCount = 4;
    
    public const int GossipMaxSize = 10485760;
    
    public const int MaxRequestLightClientUpdates = 128;
    
    public const uint GenesisForkVersion = 0x01017000;
    
    public const uint AltairForkVersion = 0x02017000;
    
    public const uint AltairForkEpoch = 0;
    
    public const uint BellatrixForkVersion = 0x03017000;
    
    public const uint BellatrixForkEpoch = 0;
    
    public const uint CapellaForkVersion = 0x04017000;
    
    public const uint CapellaForkEpoch = 256;
    
    public const uint DenebForkVersion = 0x05017000;
    
    public const uint DenebForkEpoch = 29696;
    
    public const uint ElectraForkVersion = 0x05000000;
}