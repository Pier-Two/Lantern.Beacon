using Cortex.Containers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Altair;

namespace Lantern.Beacon.Sync.Helpers;

public static class AltairHelpers
{
    public static bool IsValidLightClientHeader(AltairLightClientHeader altairLightClient)
    {
        return true;
    }

    public static bool IsSyncCommitteeUpdate(AltairLightClientUpdate lightClientUpdate)
    {
        return Phase0Helpers.IsBranchUpdate(lightClientUpdate.NextSyncCommitteeBranch);
    }

    public static bool IsFinalityUpdate(AltairLightClientUpdate lightClientUpdate)
    {
        return Phase0Helpers.IsBranchUpdate(lightClientUpdate.FinalityBranch);
    }

    public static bool IsBetterUpdate(AltairLightClientUpdate newUpdate, AltairLightClientUpdate oldUpdate)
    {
        var maxActiveParticipants = newUpdate.SyncAggregate.SyncCommitteeBits.Count;
        var newNumActiveParticipants = newUpdate.SyncAggregate.SyncCommitteeBits.Count(b => b);
        var oldNumActiveParticipants = oldUpdate.SyncAggregate.SyncCommitteeBits.Count(b => b);
        var newHasSuperMajority = newNumActiveParticipants * 3 >= maxActiveParticipants * 2;
        var oldHasSuperMajority = oldNumActiveParticipants * 3 >= maxActiveParticipants * 2;

        if (newHasSuperMajority != oldHasSuperMajority)
        {
            return Convert.ToInt32(newHasSuperMajority) > Convert.ToInt32(oldHasSuperMajority);
        }
        
        if (!newHasSuperMajority && newNumActiveParticipants != oldNumActiveParticipants)
        {
            return newNumActiveParticipants > oldNumActiveParticipants;
        }

        var newHasRelevantSyncCommittee = IsSyncCommitteeUpdate(newUpdate) &&
                                          ComputeSyncCommitteePeriodAtSlot(newUpdate.AttestedHeader.Beacon.Slot) ==
                                          ComputeSyncCommitteePeriodAtSlot(newUpdate.SignatureSlot);
        
        var oldHasRelevantSyncCommittee = IsSyncCommitteeUpdate(oldUpdate) &&
                                          ComputeSyncCommitteePeriodAtSlot(oldUpdate.AttestedHeader.Beacon.Slot) ==
                                          ComputeSyncCommitteePeriodAtSlot(oldUpdate.SignatureSlot);
        
        if (newHasRelevantSyncCommittee != oldHasRelevantSyncCommittee)
        {
            return newHasRelevantSyncCommittee;
        }
        
        var newHasFinality = IsFinalityUpdate(newUpdate);
        var oldHasFinality = IsFinalityUpdate(oldUpdate);
        
        if (newHasFinality != oldHasFinality)
        {
            return newHasFinality;
        }

        if (newHasFinality)
        {
            var newHasSyncCommitteeFinality = ComputeSyncCommitteePeriodAtSlot(newUpdate.FinalizedHeader.Beacon.Slot) ==
                                              ComputeSyncCommitteePeriodAtSlot(newUpdate.SignatureSlot);

            var oldHasSyncCommitteeFinality = ComputeSyncCommitteePeriodAtSlot(oldUpdate.FinalizedHeader.Beacon.Slot) ==
                                              ComputeSyncCommitteePeriodAtSlot(oldUpdate.AttestedHeader.Beacon.Slot);

            if (newHasSyncCommitteeFinality != oldHasSyncCommitteeFinality)
                return newHasSyncCommitteeFinality;
        }
        
        if(newNumActiveParticipants != oldNumActiveParticipants)
            return newNumActiveParticipants > oldNumActiveParticipants;
        
        if(newUpdate.AttestedHeader.Beacon.Slot != oldUpdate.AttestedHeader.Beacon.Slot)
            return newUpdate.AttestedHeader.Beacon.Slot < oldUpdate.AttestedHeader.Beacon.Slot;

        return newUpdate.SignatureSlot < oldUpdate.SignatureSlot;
    }

    public static bool IsNextSyncCommitteeKnown(AltairLightClientStore lightClientStore)
    {
        return !lightClientStore.NextSyncCommittee.Equals(AltairSyncCommittee.CreateDefault());
    }
    
    public static ulong GetSafetyThreshold(AltairLightClientStore lightClientStore)
    {
        var previousMaxActiveParticipants = lightClientStore.PreviousMaxActiveParticipants;
        var currentMaxActiveParticipants = lightClientStore.CurrentMaxActiveParticipants;
        var maxActiveParticipants = Math.Max(previousMaxActiveParticipants, currentMaxActiveParticipants);
        return maxActiveParticipants / 2;
    }
    
    public static ulong GetSubtreeIndex(ulong generalizedIndex)
    {
        return generalizedIndex % (1UL << (int)Math.Log(generalizedIndex, 2));
    }

    public static ulong ComputeSyncCommitteePeriodAtSlot(ulong slot)
    {
        return ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(slot));
    }
    
    public static ulong ComputeSyncCommitteePeriod(ulong epoch)
    {
        return epoch / (ulong)AltairPreset.EpochsPerSyncCommitteePeriod;
    }
}