namespace Lantern.Beacon.Sync.Config;

public class Config
{
    public static int MinGenesisTime { get; set; }
    
    public static int GenesisDelay { get; set; }
    
    public static int SecondsPerSlot { get; set; }
    
    public static int TimeToFirstByteTimeout { get; set; }
    
    public static int RespTimeout { get; set; }
    
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
    
    public static void InitializeWithMinimal()
    {
        MinGenesisTime = MinimalConfig.MinGenesisTime;
        GenesisDelay = MinimalConfig.GenesisDelay;
        SecondsPerSlot = MinimalConfig.SecondsPerSlot;
        TimeToFirstByteTimeout = MinimalConfig.TimeToFirstByteTimeout;
        RespTimeout = MinimalConfig.RespTimeout;
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
}