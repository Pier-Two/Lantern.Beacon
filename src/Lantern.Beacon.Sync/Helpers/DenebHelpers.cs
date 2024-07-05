using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using SszSharp;

namespace Lantern.Beacon.Sync.Helpers;

public static class DenebHelpers
{
    public static bool ShouldForwardFinalizedLightClientUpdate(DenebLightClientFinalityUpdate update, DenebLightClientHeader oldFinalizedHeader, ISyncProtocol syncProtocol)
    {
        bool result = default; 
        
        if(update.FinalizedHeader.Beacon.Slot > syncProtocol.PreviousLightClientFinalityUpdate.FinalizedHeader.Beacon.Slot)
        {
            result = true;
        }

        if(update.FinalizedHeader.Beacon.Slot == syncProtocol.PreviousLightClientFinalityUpdate.FinalizedHeader.Beacon.Slot)
        {
            var newNumActiveParticipants = update.SyncAggregate.SyncCommitteeBits.Count(b => b);
            var oldNumActiveParticipants = syncProtocol.PreviousLightClientFinalityUpdate.SyncAggregate.SyncCommitteeBits.Count(b => b);
            var newHasSuperMajority = newNumActiveParticipants * 3 >= update.SyncAggregate.SyncCommitteeBits.Count;
            var oldHasSuperMajority = oldNumActiveParticipants * 3 >= syncProtocol.PreviousLightClientFinalityUpdate.SyncAggregate.SyncCommitteeBits.Count;
            
            result = newHasSuperMajority && !oldHasSuperMajority;
        }

        if (Phase0Helpers.HasSufficientPropagationTimeElapsed(Phase0Helpers.SlotToDateTime(update.SignatureSlot, syncProtocol.Options.GenesisTime)))
        {
            result = true;
        }
        
        if (oldFinalizedHeader.Beacon.Slot < syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot)
        {
            result = true;
        }
        
        return result;
    }
    
    public static bool ShouldForwardLightClientOptimisticUpdate(DenebLightClientOptimisticUpdate update, DenebLightClientHeader oldOptimisticHeader, ISyncProtocol syncProtocol)
    {
        bool result = default; 
        
        if(update.AttestedHeader.Beacon.Slot > syncProtocol.PreviousLightClientOptimisticUpdate.AttestedHeader.Beacon.Slot)
        {
            result = true;
        }

        if (Phase0Helpers.HasSufficientPropagationTimeElapsed(Phase0Helpers.SlotToDateTime(update.SignatureSlot, syncProtocol.Options.GenesisTime)))
        {
            result = true;
        }
        
        if (oldOptimisticHeader.Beacon.Slot < syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot)
        {
            result = true;
        }
        
        return result;
    }
    
    public static byte[] GetLcExecutionRoot(DenebLightClientHeader header, SizePreset preset)
    {
        var epoch = Phase0Helpers.ComputeEpochAtSlot(header.Beacon.Slot);
        
        if(epoch >= Config.Config.DenebForkEpoch)
        {
            return header.Execution.GetHashTreeRoot(preset);
        }

        if (epoch >= Config.Config.CapellaForkEpoch)
        {
            var executionHeader = CapellaExecutionPayloadHeader.CreateFrom(
                header.Execution.ParentHash,
                header.Execution.FeeRecipientAddress,
                header.Execution.StateRoot,
                header.Execution.ReceiptsRoot,
                header.Execution.LogsBloom,
                header.Execution.PrevRandoa,
                header.Execution.BlockNumber,
                header.Execution.GasLimit,
                header.Execution.GasUsed,
                header.Execution.Timestamp,
                header.Execution.ExtraData,
                header.Execution.BaseFeePerGas,
                header.Execution.BlockHash,
                header.Execution.TransactionsRoot,
                header.Execution.WithdrawalsRoot);
            
            return executionHeader.GetHashTreeRoot(preset);
        }

        return new byte[Constants.RootLength];
    }
    
    public static bool IsValidLightClientHeader(DenebLightClientHeader header, SizePreset preset)
    {
        var epoch = Phase0Helpers.ComputeEpochAtSlot(header.Beacon.Slot);

        if (epoch < Config.Config.DenebForkEpoch)
        {
            if (header.Execution.ExcessBlobGas != 0 || header.Execution.BlobGasUsed != 0)
            {
                return false;
            }
        }
        
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
    
    public static bool IsSyncCommitteeUpdate(DenebLightClientUpdate lightClientUpdate)
    {
        return Phase0Helpers.IsBranchUpdate(lightClientUpdate.NextSyncCommitteeBranch);
    }

    public static bool IsFinalityUpdate(DenebLightClientUpdate lightClientUpdate)
    {
        return Phase0Helpers.IsBranchUpdate(lightClientUpdate.FinalityBranch);
    }

    public static bool IsBetterUpdate(DenebLightClientUpdate newUpdate, DenebLightClientUpdate oldUpdate)
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

    public static bool IsNextSyncCommitteeKnown(DenebLightClientStore lightClientStore)
    {
        return !lightClientStore.NextSyncCommittee.Equals(AltairSyncCommittee.CreateDefault());
    }
    
    public static ulong GetSafetyThreshold(DenebLightClientStore lightClientStore)
    {
        var previousMaxActiveParticipants = lightClientStore.PreviousMaxActiveParticipants;
        var currentMaxActiveParticipants = lightClientStore.CurrentMaxActiveParticipants;
        var maxActiveParticipants = Math.Max(previousMaxActiveParticipants, currentMaxActiveParticipants);
        return maxActiveParticipants / 2;
    }
}