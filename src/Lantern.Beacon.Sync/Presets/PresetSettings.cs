namespace Lantern.Beacon.Sync.Presets;

public class PresetSettings
{
    public int SlotsPerEpoch { get; set; }
    
    public int SyncCommitteeSize { get; set; }
    
    public int EpochsPerSyncCommitteePeriod { get; set; }
    
    public int MinSyncCommitteeParticipants { get; set; }
}