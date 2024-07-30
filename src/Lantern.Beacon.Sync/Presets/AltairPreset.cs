namespace Lantern.Beacon.Sync.Presets;

public static class AltairPreset
{
    public static int SyncCommitteeSize { get; set; }
    public static int EpochsPerSyncCommitteePeriod { get; set; }
    public static int MinSyncCommitteeParticipants { get; set; }
    public static int UpdateTimeout => EpochsPerSyncCommitteePeriod * Phase0Preset.SlotsPerEpoch;

    public static void InitializeWithMainnet()
    {
        SyncCommitteeSize = Mainnet.AltairPresetValues.SyncCommitteeSize;
        EpochsPerSyncCommitteePeriod = Mainnet.AltairPresetValues.EpochsPerSyncCommitteePeriod;
        MinSyncCommitteeParticipants = Mainnet.AltairPresetValues.MinSyncCommitteeParticipants;
    }

    public static void InitializeWithHolesky()
    {
        SyncCommitteeSize = Holesky.AltairPresetValues.SyncCommitteeSize;
        EpochsPerSyncCommitteePeriod = Holesky.AltairPresetValues.EpochsPerSyncCommitteePeriod;
        MinSyncCommitteeParticipants = Holesky.AltairPresetValues.MinSyncCommitteeParticipants;
    }
    
    public static void InitializeWithMinimal()
    {
        SyncCommitteeSize = Minimal.AltairPresetValues.SyncCommitteeSize;
        EpochsPerSyncCommitteePeriod = Minimal.AltairPresetValues.EpochsPerSyncCommitteePeriod;
        MinSyncCommitteeParticipants = Minimal.AltairPresetValues.MinSyncCommitteeParticipants;
    }
    
    public static void InitializeWithCustom(PresetSettings preset)
    {
        SyncCommitteeSize = preset.SyncCommitteeSize;
        EpochsPerSyncCommitteePeriod = preset.EpochsPerSyncCommitteePeriod;
        MinSyncCommitteeParticipants = preset.MinSyncCommitteeParticipants;
    }
}