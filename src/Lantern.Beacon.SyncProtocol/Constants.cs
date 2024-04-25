namespace Lantern.Beacon.SyncProtocol;

public static class Constants
{
    public const int SyncCommitteeSize = 512;

    public const int FinalizedRootGIndex = 105;
    
    public const int FinalityBranchDepth = 6;

    public const int CurrentSyncCommitteeGIndex = 54;

    public const int CurrentSyncCommitteeBranchDepth = 5;
    
    public const int NextSyncCommitteeGIndex = 55;
    
    public const int NextSyncCommitteeBranchDepth = 5;

    public const int ExecutionPayloadGIndex = 25;
    
    public const int ExecutionBranchDepth = 4;

    public const int BytesPerLogsBloom = 256;
    
    public const int MaxExtraDataBytes = 32;
    
    public const int BytesPerLengthOffset = 4;
}