namespace Lantern.Beacon.SyncProtocol.Types;

public class LightClientStore(LightClientHeader finalizedHeader,
    SyncCommittee currentSyncCommittee,
    SyncCommittee nextSyncCommittee,
    LightClientUpdate? bestValidUpdate,
    LightClientHeader optimisticHeader,
    uint previousMaxParticipants,
    uint currentMaxParticipants) : IEquatable<LightClientStore>
{
    public LightClientHeader FinalizedHeader { get; init; } = finalizedHeader;
        
    public SyncCommittee CurrentSyncCommittee { get; init; } = currentSyncCommittee;
        
    public SyncCommittee NextSyncCommittee { get; init; } = nextSyncCommittee;
        
    public LightClientUpdate? BestValidUpdate { get; init; } = bestValidUpdate;
        
    public LightClientHeader OptimisticHeader { get; init; } = optimisticHeader;
        
    public ulong PreviousMaxParticipants { get; init; } = previousMaxParticipants;
        
    public ulong CurrentMaxParticipants { get; init; } = currentMaxParticipants;
        
    public bool Equals(LightClientStore? other)
    {
        if (other == null)
            return false;

        if (!FinalizedHeader.Equals(other.FinalizedHeader) ||
            !CurrentSyncCommittee.Equals(other.CurrentSyncCommittee) ||
            !NextSyncCommittee.Equals(other.NextSyncCommittee) ||
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
        if (obj is LightClientStore other)
        {
            return Equals(other);
        }
            
        return false;
    }
        
    public override int GetHashCode()
    {
        return HashCode.Combine(FinalizedHeader, CurrentSyncCommittee, NextSyncCommittee, BestValidUpdate, OptimisticHeader, PreviousMaxParticipants, CurrentMaxParticipants);
    }
        
    public static LightClientStore CreateDefault()
    {
        return new LightClientStore(LightClientHeader.CreateDefault(), SyncCommittee.CreateDefault(), SyncCommittee.CreateDefault(), null, LightClientHeader.CreateDefault(), 0, 0);
    }
    
    public static class Serializer
    {
        public static byte[] Serialize(LightClientStore store)
        {
            throw new NotImplementedException();
        }
            
        public static LightClientStore Deserialize(byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}