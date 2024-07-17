using Lantern.Beacon.Sync.Types.Ssz.Altair;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Capella;

public class CapellaLightClientStore : IEquatable<CapellaLightClientStore>
{
    public CapellaLightClientHeader FinalizedHeader { get; internal set; } 
        
    public AltairSyncCommittee CurrentSyncCommittee { get;  internal set; }
        
    public AltairSyncCommittee NextSyncCommittee { get; internal set; }
        
    public CapellaLightClientUpdate? BestValidUpdate { get; internal set; } 
        
    public CapellaLightClientHeader OptimisticHeader { get; internal set; } 
        
    public ulong PreviousMaxActiveParticipants { get; internal set; } 
        
    public ulong CurrentMaxActiveParticipants { get; internal set; } 
    
    public bool Equals(CapellaLightClientStore? other)
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
        if (obj is CapellaLightClientStore other)
        {
            return Equals(other);
        }
            
        return false;
    }
        
    public override int GetHashCode()
    {
        return HashCode.Combine(FinalizedHeader, CurrentSyncCommittee, NextSyncCommittee, BestValidUpdate, OptimisticHeader, PreviousMaxActiveParticipants, CurrentMaxActiveParticipants);
    }
        
    public static CapellaLightClientStore CreateFromAltair(AltairLightClientStore pre)
    {
        CapellaLightClientUpdate? bestValidUpdate;
        
        if (pre.BestValidUpdate == null)
        {
            bestValidUpdate = null;
        }
        else
        {
            bestValidUpdate = CapellaLightClientUpdate.CreateFromAltair(pre.BestValidUpdate);
        }

        return CapellaLightClientStore.CreateFrom(
            CapellaLightClientHeader.CreateFromAltair(pre.FinalizedHeader),
            pre.CurrentSyncCommittee,
            pre.NextSyncCommittee,
            bestValidUpdate,
            CapellaLightClientHeader.CreateFromAltair(pre.OptimisticHeader),
            pre.PreviousMaxActiveParticipants,
            pre.CurrentMaxActiveParticipants);
    }
    
    public static CapellaLightClientStore CreateDefault()
    {
        return CapellaLightClientStore.CreateFrom(CapellaLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), CapellaLightClientUpdate.CreateDefault(), CapellaLightClientHeader.CreateDefault(), 0, 0);
    }

    public static CapellaLightClientStore CreateFrom(CapellaLightClientHeader finalizedHeader,
        AltairSyncCommittee currentAltairSyncCommittee,
        AltairSyncCommittee nextAltairSyncCommittee,
        CapellaLightClientUpdate? bestValidUpdate,
        CapellaLightClientHeader optimisticHeader,
        ulong previousMaxActiveParticipants,
        ulong currentMaxActiveParticipants)
    {
        return new CapellaLightClientStore
        {
            CurrentSyncCommittee = currentAltairSyncCommittee,
            FinalizedHeader = finalizedHeader,
            NextSyncCommittee = nextAltairSyncCommittee,
            BestValidUpdate = bestValidUpdate,
            OptimisticHeader = optimisticHeader,
            PreviousMaxActiveParticipants = previousMaxActiveParticipants,
            CurrentMaxActiveParticipants = currentMaxActiveParticipants
        };
    }
    
    public static byte[] Serialize(CapellaLightClientStore capellaLightClientStore, SizePreset preset)
    {
        return SszContainer.Serialize(capellaLightClientStore, preset);
    }
    
    public static CapellaLightClientStore Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<CapellaLightClientStore>(data, preset);
        return result.Item1;
    } 
}