using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientStore : IEquatable<DenebLightClientStore>
{
    public DenebLightClientHeader FinalizedHeader { get; internal set; }
        
    public AltairSyncCommittee CurrentSyncCommittee { get;  internal set; } 
        
    public AltairSyncCommittee NextSyncCommittee { get; internal set; } 
        
    public DenebLightClientUpdate? BestValidUpdate { get; internal set; } 
        
    public DenebLightClientHeader OptimisticHeader { get; internal set; } 
    
    public ulong PreviousMaxActiveParticipants { get; internal set; } 
        
    public ulong CurrentMaxActiveParticipants { get; internal set; } 
    
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

        return CreateFrom(
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
        return CreateFrom(DenebLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), AltairSyncCommittee.CreateDefault(), DenebLightClientUpdate.CreateDefault(), DenebLightClientHeader.CreateDefault(), 0, 0);
    }

    public static DenebLightClientStore CreateFrom(DenebLightClientHeader finalizedHeader,
        AltairSyncCommittee currentAltairSyncCommittee,
        AltairSyncCommittee nextAltairSyncCommittee,
        DenebLightClientUpdate? bestValidUpdate,
        DenebLightClientHeader optimisticHeader,
        ulong previousMaxActiveParticipants,
        ulong currentMaxActiveParticipants)
    {
        return new DenebLightClientStore
        {
            CurrentSyncCommittee = currentAltairSyncCommittee,
            NextSyncCommittee = nextAltairSyncCommittee,
            BestValidUpdate = bestValidUpdate,
            FinalizedHeader = finalizedHeader,
            OptimisticHeader = optimisticHeader,
            PreviousMaxActiveParticipants = previousMaxActiveParticipants,
            CurrentMaxActiveParticipants = currentMaxActiveParticipants
        };
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