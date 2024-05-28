namespace Lantern.Beacon.Sync;

public static class DomainTypes
{
    public const uint DomainBeaconProposer = 0x00000000; 
    
    public const uint DomainBeaconAttester = 0x01000000;
    
    public const uint DomainRandao = 0x02000000;
    
    public const uint DomainDeposit = 0x03000000;
    
    public const uint DomainVoluntaryExit = 0x04000000;
    
    public const uint DomainSelectionProof = 0x05000000;
    
    public const uint DomainAggregateAndProof = 0x06000000;
    
    public const uint DomainApplicationMask = 0x00000001;
    
    public const uint DomainSyncCommittee = 0x07000000;
}