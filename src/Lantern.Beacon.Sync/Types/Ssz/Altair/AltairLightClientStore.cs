using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Altair;

public class AltairLightClientStore(AltairLightClientHeader finalizedHeader,
    AltairSyncCommittee currentAltairSyncCommittee,
    AltairSyncCommittee nextAltairSyncCommittee,
    AltairLightClientUpdate? bestValidUpdate,
    AltairLightClientHeader optimisticHeader,
    ulong previousMaxActiveParticipants,
    ulong currentMaxActiveParticipants) : IEquatable<AltairLightClientStore>
{
    public AltairLightClientHeader FinalizedHeader { get; internal set; } = finalizedHeader;
        
    public AltairSyncCommittee CurrentSyncCommittee { get; internal set; } = currentAltairSyncCommittee;
        
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } = nextAltairSyncCommittee;
        
    public AltairLightClientUpdate? BestValidUpdate { get; internal set; } = bestValidUpdate;
        
    public AltairLightClientHeader OptimisticHeader { get; internal set; } = optimisticHeader;
        
    public ulong PreviousMaxActiveParticipants { get; internal set; } = previousMaxActiveParticipants;
        
    public ulong CurrentMaxActiveParticipants { get; internal set; } = currentMaxActiveParticipants;
    
    public bool Equals(AltairLightClientStore? other)
    {
        if (other == null)
            return false;

        if (!FinalizedHeader.Equals(other.FinalizedHeader) ||
            !CurrentSyncCommittee.Equals(other.CurrentSyncCommittee) ||
            !NextSyncCommittee.Equals(other.NextSyncCommittee) ||
            !OptimisticHeader.Equals(other.OptimisticHeader) ||
            PreviousMaxActiveParticipants != other.PreviousMaxActiveParticipants ||
            CurrentMaxActiveParticipants != other.CurrentMaxActiveParticipants)
        {
            return false;
        }
        
        if (BestValidUpdate == null && other.BestValidUpdate == null)
            return true;
        if (BestValidUpdate == null || other.BestValidUpdate == null)
            return false;

        return BestValidUpdate.Equals(other.BestValidUpdate);
    }
        
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientStore other)
        {
            return Equals(other);
        }
            
        return false;
    }
        
    public override int GetHashCode()
    {
        return HashCode.Combine(FinalizedHeader, CurrentSyncCommittee, NextSyncCommittee, BestValidUpdate, OptimisticHeader, PreviousMaxActiveParticipants, CurrentMaxActiveParticipants);
    }
        
    public static AltairLightClientStore CreateDefault()
    {
        return new AltairLightClientStore(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairLightClientUpdate.CreateDefault(), AltairLightClientHeader.CreateDefault(), 0, 0);
    }
    
    public static byte[] Serialize(AltairLightClientStore altairLightClientStore, SizePreset preset)
    {
        return SszContainer.Serialize(altairLightClientStore, preset);
    }
    
    public static AltairLightClientStore Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<AltairLightClientStore>(data, preset);
        return result.Item1;
    } 
}