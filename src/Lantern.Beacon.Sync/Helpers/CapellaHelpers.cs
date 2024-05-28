using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Nethereum.Hex.HexConvertors.Extensions;
using SszSharp;

namespace Lantern.Beacon.Sync.Helpers;

public static class CapellaHelpers
{
    public static byte[] GetLcExecutionRoot(CapellaLightClientHeader header, SizePreset preset)
    {
        var epoch = Phase0Helpers.ComputeEpochAtSlot(header.Beacon.Slot);
        
        if(epoch >= Config.Config.CapellaForkEpoch)
        {
            return header.Execution.GetHashTreeRoot(preset);
        }
        
        return new byte[Constants.RootLength];
    }

    public static bool IsValidLightClientHeader(CapellaLightClientHeader header, SizePreset preset)
    {
        var epoch = Phase0Helpers.ComputeEpochAtSlot(header.Beacon.Slot);
        
        if (epoch < Config.Config.CapellaForkEpoch)
        {
            var executionBranch = new byte[Constants.ExecutionBranchDepth][];
            
            for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
            {
                executionBranch[i] = new byte[Constants.RootLength];
            }

            return header.Execution.Equals(CapellaExecutionPayloadHeader.CreateDefault()) &&
                   Phase0Helpers.AreBranchesEqual(header.ExecutionBranch, executionBranch);
        }

        var leaf = GetLcExecutionRoot(header, preset);
        var branch = header.ExecutionBranch;
        var depth = (int)Math.Log(Constants.ExecutionPayloadGIndex, 2);
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.ExecutionPayloadGIndex);
        var root = header.Beacon.BodyRoot;
        
        return Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root);
    }
    
    public static bool IsSyncCommitteeUpdate(CapellaLightClientUpdate lightClientUpdate)
    {
        return Phase0Helpers.IsBranchUpdate(lightClientUpdate.NextSyncCommitteeBranch);
    }

    public static bool IsFinalityUpdate(CapellaLightClientUpdate lightClientUpdate)
    {
        return Phase0Helpers.IsBranchUpdate(lightClientUpdate.FinalityBranch);
    }

    public static bool IsBetterUpdate(CapellaLightClientUpdate newUpdate, CapellaLightClientUpdate oldUpdate)
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
                                          AltairHelpers.ComputeSyncCommitteePeriodAtSlot(newUpdate.AttestedHeader.Beacon.Slot) ==
                                          AltairHelpers.ComputeSyncCommitteePeriodAtSlot(newUpdate.SignatureSlot);
        
        var oldHasRelevantSyncCommittee = IsSyncCommitteeUpdate(oldUpdate) &&
                                          AltairHelpers.ComputeSyncCommitteePeriodAtSlot(oldUpdate.AttestedHeader.Beacon.Slot) ==
                                          AltairHelpers.ComputeSyncCommitteePeriodAtSlot(oldUpdate.SignatureSlot);
        
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
            var newHasSyncCommitteeFinality = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(newUpdate.FinalizedHeader.Beacon.Slot) ==
                                              AltairHelpers.ComputeSyncCommitteePeriodAtSlot(newUpdate.SignatureSlot);

            var oldHasSyncCommitteeFinality = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(oldUpdate.FinalizedHeader.Beacon.Slot) ==
                                              AltairHelpers.ComputeSyncCommitteePeriodAtSlot(oldUpdate.AttestedHeader.Beacon.Slot);

            if (newHasSyncCommitteeFinality != oldHasSyncCommitteeFinality)
                return newHasSyncCommitteeFinality;
        }
        
        if(newNumActiveParticipants != oldNumActiveParticipants)
            return newNumActiveParticipants > oldNumActiveParticipants;
        
        if(newUpdate.AttestedHeader.Beacon.Slot != oldUpdate.AttestedHeader.Beacon.Slot)
            return newUpdate.AttestedHeader.Beacon.Slot < oldUpdate.AttestedHeader.Beacon.Slot;

        return newUpdate.SignatureSlot < oldUpdate.SignatureSlot;
    }

    public static bool IsNextSyncCommitteeKnown(CapellaLightClientStore lightClientStore)
    {
        return !lightClientStore.NextSyncCommittee.Equals(AltairSyncCommittee.CreateDefault());
    }
    
    public static ulong GetSafetyThreshold(CapellaLightClientStore lightClientStore)
    {
        var previousMaxActiveParticipants = lightClientStore.PreviousMaxActiveParticipants;
        var currentMaxActiveParticipants = lightClientStore.CurrentMaxActiveParticipants;
        var maxActiveParticipants = Math.Max(previousMaxActiveParticipants, currentMaxActiveParticipants);
        return maxActiveParticipants / 2;
    }
}