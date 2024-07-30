namespace Lantern.Beacon.Sync.Config;

public class Config
{
    public static int MinGenesisTime { get; set; }
    
    public static int GenesisDelay { get; set; }
    
    public static int SecondsPerSlot { get; set; }
    
    public static int TimeToFirstByteTimeout { get; set; }
    
    public static int RespTimeout { get; set; }
    
    public static int AttestationSubnetCount { get; set; }
    
    public static int SyncCommitteeSubnetCount { get; set; }
    
    public static int GossipMaxSize { get; set; }
    
    public static int MaxRequestLightClientUpdates { get; set; }
    
    public static uint GenesisForkVersion { get; set; }
    
    public static uint AltairForkVersion { get; set; }

    public static uint AltairForkEpoch { get; set; }
    
    public static uint BellatrixForkVersion { get; set; }
    
    public static uint BellatrixForkEpoch { get; set; }
    
    public static uint CapellaForkVersion { get; set; }
    
    public static uint CapellaForkEpoch { get; set; }
    
    public static uint DenebForkVersion { get; set; }
    
    public static uint DenebForkEpoch { get; set; }
    
    public static uint ElectraForkVersion { get; set; }
    
    public static void InitializeWithMainnet()
    {
        MinGenesisTime = MainnetConfig.MinGenesisTime;
        GenesisDelay = MainnetConfig.GenesisDelay;
        SecondsPerSlot = MainnetConfig.SecondsPerSlot;
        TimeToFirstByteTimeout = MainnetConfig.TimeToFirstByteTimeout;
        RespTimeout = MainnetConfig.RespTimeout;
        AttestationSubnetCount = MainnetConfig.AttestationSubnetCount;
        SyncCommitteeSubnetCount = MainnetConfig.SyncCommitteeSubnetCount;
        GossipMaxSize = MainnetConfig.GossipMaxSize;
        MaxRequestLightClientUpdates = MainnetConfig.MaxRequestLightClientUpdates;
        AltairForkVersion = MainnetConfig.AltairForkVersion;
        AltairForkEpoch = MainnetConfig.AltairForkEpoch;
        BellatrixForkVersion = MainnetConfig.BellatrixForkVersion;
        BellatrixForkEpoch = MainnetConfig.BellatrixForkEpoch;
        CapellaForkVersion = MainnetConfig.CapellaForkVersion;
        CapellaForkEpoch = MainnetConfig.CapellaForkEpoch;
        DenebForkVersion = MainnetConfig.DenebForkVersion;
        DenebForkEpoch = MainnetConfig.DenebForkEpoch;
        ElectraForkVersion = MainnetConfig.ElectraForkVersion;
        GenesisForkVersion = MainnetConfig.GenesisForkVersion;
    }
    
    public static void InitializeWithHolesky()
    {
        MinGenesisTime = HoleskyConfig.MinGenesisTime;
        GenesisDelay = HoleskyConfig.GenesisDelay;
        SecondsPerSlot = HoleskyConfig.SecondsPerSlot;
        TimeToFirstByteTimeout = HoleskyConfig.TimeToFirstByteTimeout;
        RespTimeout = HoleskyConfig.RespTimeout;
        AttestationSubnetCount = HoleskyConfig.AttestationSubnetCount;
        SyncCommitteeSubnetCount = HoleskyConfig.SyncCommitteeSubnetCount;
        GossipMaxSize = HoleskyConfig.GossipMaxSize;
        MaxRequestLightClientUpdates = HoleskyConfig.MaxRequestLightClientUpdates;
        AltairForkVersion = HoleskyConfig.AltairForkVersion;
        AltairForkEpoch = HoleskyConfig.AltairForkEpoch;
        BellatrixForkVersion = HoleskyConfig.BellatrixForkVersion;
        BellatrixForkEpoch = HoleskyConfig.BellatrixForkEpoch;
        CapellaForkVersion = HoleskyConfig.CapellaForkVersion;
        CapellaForkEpoch = HoleskyConfig.CapellaForkEpoch;
        DenebForkVersion = HoleskyConfig.DenebForkVersion;
        DenebForkEpoch = HoleskyConfig.DenebForkEpoch;
        ElectraForkVersion = HoleskyConfig.ElectraForkVersion;
        GenesisForkVersion = HoleskyConfig.GenesisForkVersion;
    }

    public static void InitializeWithMinimal()
    {
        MinGenesisTime = MinimalConfig.MinGenesisTime;
        GenesisDelay = MinimalConfig.GenesisDelay;
        SecondsPerSlot = MinimalConfig.SecondsPerSlot;
        TimeToFirstByteTimeout = MinimalConfig.TimeToFirstByteTimeout;
        RespTimeout = MinimalConfig.RespTimeout;
        AttestationSubnetCount = MinimalConfig.AttestationSubnetCount;
        SyncCommitteeSubnetCount = MinimalConfig.SyncCommitteeSubnetCount;
        GossipMaxSize = MinimalConfig.GossipMaxSize;
        MaxRequestLightClientUpdates = MinimalConfig.MaxRequestLightClientUpdates;
        AltairForkVersion = MinimalConfig.AltairForkVersion;
        AltairForkEpoch = MinimalConfig.AltairForkEpoch;
        BellatrixForkVersion = MinimalConfig.BellatrixForkVersion;
        BellatrixForkEpoch = MinimalConfig.BellatrixForkEpoch;
        CapellaForkVersion = MinimalConfig.CapellaForkVersion;
        CapellaForkEpoch = MinimalConfig.CapellaForkEpoch;
        DenebForkVersion = MinimalConfig.DenebForkVersion;
        DenebForkEpoch = MinimalConfig.DenebForkEpoch;
        ElectraForkVersion = MinimalConfig.ElectraForkVersion;
        GenesisForkVersion = MinimalConfig.GenesisForkVersion;
    }
    
    public static void InitializeWithCustom(ConfigSettings config)
    {
        MinGenesisTime = config.MinGenesisTime;
        GenesisDelay = config.GenesisDelay;
        SecondsPerSlot = config.SecondsPerSlot;
        TimeToFirstByteTimeout = config.TimeToFirstByteTimeout;
        RespTimeout = config.RespTimeout;
        AttestationSubnetCount = config.AttestationSubnetCount;
        SyncCommitteeSubnetCount = config.SyncCommitteeSubnetCount;
        GossipMaxSize = config.GossipMaxSize;
        MaxRequestLightClientUpdates = config.MaxRequestLightClientUpdates;
        AltairForkVersion = config.AltairForkVersion;
        AltairForkEpoch = config.AltairForkEpoch;
        BellatrixForkVersion = config.BellatrixForkVersion;
        BellatrixForkEpoch = config.BellatrixForkEpoch;
        CapellaForkVersion = config.CapellaForkVersion;
        CapellaForkEpoch = config.CapellaForkEpoch;
        DenebForkVersion = config.DenebForkVersion;
        DenebForkEpoch = config.DenebForkEpoch;
        ElectraForkVersion = config.ElectraForkVersion;
        GenesisForkVersion = config.GenesisForkVersion;
    }
}