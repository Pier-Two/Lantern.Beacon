using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Altair;

public class AltairLightClientStore : IEquatable<AltairLightClientStore>
{
    public AltairLightClientHeader FinalizedHeader { get; internal set; } 
        
    public AltairSyncCommittee CurrentSyncCommittee { get; internal set; }
        
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } 
        
    public AltairLightClientUpdate? BestValidUpdate { get; internal set; } 
        
    public AltairLightClientHeader OptimisticHeader { get; internal set; } 
        
    public ulong PreviousMaxActiveParticipants { get; internal set; } 
        
    public ulong CurrentMaxActiveParticipants { get; internal set; } 
    
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
        return CreateFrom(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairLightClientUpdate.CreateDefault(), AltairLightClientHeader.CreateDefault(), 0, 0);
    }

    public static AltairLightClientStore CreateFrom(AltairLightClientHeader finalizedHeader,
        AltairSyncCommittee currentAltairSyncCommittee,
        AltairSyncCommittee nextAltairSyncCommittee,
        AltairLightClientUpdate? bestValidUpdate,
        AltairLightClientHeader optimisticHeader,
        ulong previousMaxActiveParticipants,
        ulong currentMaxActiveParticipants)
    {
        return new AltairLightClientStore
        {
            FinalizedHeader = finalizedHeader,
            CurrentSyncCommittee = currentAltairSyncCommittee,
            NextSyncCommittee = nextAltairSyncCommittee,
            BestValidUpdate = bestValidUpdate,
            OptimisticHeader = optimisticHeader,
            PreviousMaxActiveParticipants = previousMaxActiveParticipants,
            CurrentMaxActiveParticipants = currentMaxActiveParticipants
        };
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