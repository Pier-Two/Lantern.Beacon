using Lantern.Beacon.Sync.Types.Ssz.Altair;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Capella;

public class CapellaLightClientStore(CapellaLightClientHeader finalizedHeader,
    AltairSyncCommittee currentAltairSyncCommittee,
    AltairSyncCommittee nextAltairSyncCommittee,
    CapellaLightClientUpdate? bestValidUpdate,
    CapellaLightClientHeader optimisticHeader,
    ulong previousMaxActiveParticipants,
    ulong currentMaxActiveParticipants) : IEquatable<CapellaLightClientStore>
{
    public CapellaLightClientHeader FinalizedHeader { get; internal set; } = finalizedHeader;
        
    public AltairSyncCommittee CurrentSyncCommittee { get;  internal set; } = currentAltairSyncCommittee;
        
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } = nextAltairSyncCommittee;
        
    public CapellaLightClientUpdate? BestValidUpdate { get; internal set; } = bestValidUpdate;
        
    public CapellaLightClientHeader OptimisticHeader { get; internal set; } = optimisticHeader;
        
    public ulong PreviousMaxActiveParticipants { get; internal set; } = previousMaxActiveParticipants;
        
    public ulong CurrentMaxActiveParticipants { get; internal set; } = currentMaxActiveParticipants;
    
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

        return new CapellaLightClientStore(
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
        return new CapellaLightClientStore(CapellaLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), CapellaLightClientUpdate.CreateDefault(), CapellaLightClientHeader.CreateDefault(), 0, 0);
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