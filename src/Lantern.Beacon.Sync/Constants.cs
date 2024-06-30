using System.Numerics;

namespace Lantern.Beacon.Sync;

public static class Constants
{
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
    
    public const int RootLength = 32;
    
    public const int VersionLength = 4;
    
    public const int ContextBytesLength = 4;
    
    public const int DomainLength = 32;

    public const int MaxLightClientUpdates = 128;

    public const ulong FarFutureEpoch = ulong.MaxValue;
}