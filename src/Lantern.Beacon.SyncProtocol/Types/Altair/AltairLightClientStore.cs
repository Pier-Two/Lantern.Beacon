namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientStore(AltairLightClientHeader finalizedHeader,
    AltairSyncCommittee currentAltairSyncCommittee,
    AltairSyncCommittee nextAltairSyncCommittee,
    AltairLightClientUpdate? bestValidUpdate,
    AltairLightClientHeader optimisticHeader,
    uint previousMaxParticipants,
    uint currentMaxParticipants) : IEquatable<AltairLightClientStore>
{
    public AltairLightClientHeader FinalizedHeader { get; init; } = finalizedHeader;
        
    public AltairSyncCommittee CurrentAltairSyncCommittee { get; init; } = currentAltairSyncCommittee;
        
    public AltairSyncCommittee NextAltairSyncCommittee { get; init; } = nextAltairSyncCommittee;
        
    public AltairLightClientUpdate? BestValidUpdate { get; init; } = bestValidUpdate;
        
    public AltairLightClientHeader OptimisticHeader { get; init; } = optimisticHeader;
        
    public ulong PreviousMaxParticipants { get; init; } = previousMaxParticipants;
        
    public ulong CurrentMaxParticipants { get; init; } = currentMaxParticipants;
        
    public bool Equals(AltairLightClientStore? other)
    {
        if (other == null)
            return false;

        if (!FinalizedHeader.Equals(other.FinalizedHeader) ||
            !CurrentAltairSyncCommittee.Equals(other.CurrentAltairSyncCommittee) ||
            !NextAltairSyncCommittee.Equals(other.NextAltairSyncCommittee) ||
            !OptimisticHeader.Equals(other.OptimisticHeader) ||
            PreviousMaxParticipants != other.PreviousMaxParticipants ||
            CurrentMaxParticipants != other.CurrentMaxParticipants)
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
        return HashCode.Combine(FinalizedHeader, CurrentAltairSyncCommittee, NextAltairSyncCommittee, BestValidUpdate, OptimisticHeader, PreviousMaxParticipants, CurrentMaxParticipants);
    }
        
    public static AltairLightClientStore CreateDefault()
    {
        return new AltairLightClientStore(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), null, AltairLightClientHeader.CreateDefault(), 0, 0);
    }
}