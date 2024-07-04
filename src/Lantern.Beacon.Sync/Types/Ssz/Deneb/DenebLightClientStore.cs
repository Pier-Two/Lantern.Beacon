using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientStore(DenebLightClientHeader finalizedHeader,
    AltairSyncCommittee currentAltairSyncCommittee,
    AltairSyncCommittee nextAltairSyncCommittee,
    DenebLightClientUpdate? bestValidUpdate,
    DenebLightClientHeader optimisticHeader,
    ulong previousMaxActiveParticipants,
    ulong currentMaxActiveParticipants) : IEquatable<DenebLightClientStore>
{
    public DenebLightClientHeader FinalizedHeader { get; internal set; } = finalizedHeader;
        
    public AltairSyncCommittee CurrentSyncCommittee { get;  internal set; } = currentAltairSyncCommittee;
        
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } = nextAltairSyncCommittee;
        
    public DenebLightClientUpdate? BestValidUpdate { get; internal set; } = bestValidUpdate;
        
    public DenebLightClientHeader OptimisticHeader { get; internal set; } = optimisticHeader;
        
    public ulong PreviousMaxActiveParticipants { get; internal set; } = previousMaxActiveParticipants;
        
    public ulong CurrentMaxActiveParticipants { get; internal set; } = currentMaxActiveParticipants;
    
    public bool Equals(DenebLightClientStore? other)
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
        if (obj is DenebLightClientStore other)
        {
            return Equals(other);
        }
            
        return false;
    }
        
    public override int GetHashCode()
    {
        return HashCode.Combine(FinalizedHeader, CurrentSyncCommittee, NextSyncCommittee, BestValidUpdate, OptimisticHeader, PreviousMaxActiveParticipants, CurrentMaxActiveParticipants);
    }
    
    public static DenebLightClientStore CreateFromCapella(CapellaLightClientStore pre)
    {
        DenebLightClientUpdate? bestValidUpdate;
        
        if (pre.BestValidUpdate == null)
        {
            bestValidUpdate = null;
        }
        else
        {
            bestValidUpdate = DenebLightClientUpdate.CreateFromCapella(pre.BestValidUpdate);
        }

        return new DenebLightClientStore(
            DenebLightClientHeader.CreateFromCapella(pre.FinalizedHeader),
            pre.CurrentSyncCommittee,
            pre.NextSyncCommittee,
            bestValidUpdate,
            DenebLightClientHeader.CreateFromCapella(pre.OptimisticHeader),
            pre.PreviousMaxActiveParticipants,
            pre.CurrentMaxActiveParticipants);
    }
        
    public static DenebLightClientStore CreateDefault()
    {
        return new DenebLightClientStore(DenebLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), DenebLightClientUpdate.CreateDefault(), DenebLightClientHeader.CreateDefault(), 0, 0);
    }
    
    public static byte[] Serialize(DenebLightClientStore denebLightClientStore, SizePreset preset)
    {
        return SszContainer.Serialize(denebLightClientStore, preset);
    }
    
    public static DenebLightClientStore Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<DenebLightClientStore>(data, preset);
        return result.Item1;
    } 
}