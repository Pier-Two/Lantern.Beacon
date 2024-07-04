using Cortex.Containers;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;
using Planetarium.Cryptography.BLS12_381;

namespace Lantern.Beacon.Sync.Processors;

public static class DenebProcessors
{
    private static bool ValidateLightClientUpdate(DenebLightClientStore store, DenebLightClientUpdate update, ulong currentSlot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        var syncAggregate = update.SyncAggregate;

        if (!(syncAggregate.SyncCommitteeBits.Count(b => b) >= AltairPreset.MinSyncCommitteeParticipants))
        {
            logger.LogWarning("Sync aggregate has insufficient active participants in update");
            return false;
        }

        if (!DenebHelpers.IsValidLightClientHeader(update.AttestedHeader, options.Preset))
        {
            logger.LogWarning("Invalid attested header in update");
            return false;
        }

        var updateAttestedSlot = update.AttestedHeader.Beacon.Slot;
        var updateFinalizedSlot = update.FinalizedHeader.Beacon.Slot;

        if (!(currentSlot >= update.SignatureSlot && update.SignatureSlot > updateAttestedSlot && updateAttestedSlot >= updateFinalizedSlot))
        {
            logger.LogWarning("Invalid slot values in update");
            return false;
        }

        var storePeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(store.FinalizedHeader.Beacon.Slot);
        var updateSignaturePeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.SignatureSlot);

        if (DenebHelpers.IsNextSyncCommitteeKnown(store))
        {
            if (!(updateSignaturePeriod == storePeriod || updateSignaturePeriod == storePeriod + 1))
            {
                logger.LogWarning("Invalid sync committee period in update");
                return false;
            }
        }
        else
        {
            if (updateSignaturePeriod != storePeriod)
            {
                logger.LogWarning("Invalid sync committee period in update");
                return false;
            }
        }

        var updateAttestedPeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(updateAttestedSlot);
        var updateHasNextSyncCommittee = !DenebHelpers.IsNextSyncCommitteeKnown(store) && 
                                         DenebHelpers.IsSyncCommitteeUpdate(update) && updateAttestedPeriod == storePeriod;

        if (!(updateAttestedSlot > store.FinalizedHeader.Beacon.Slot || updateHasNextSyncCommittee))
        {
            logger.LogWarning("Update is older than finalised slot");
            return false;
        }

        if (!DenebHelpers.IsFinalityUpdate(update))
        {
            if (!update.FinalizedHeader.GetHashTreeRoot(options.Preset).SequenceEqual(DenebLightClientHeader.CreateDefault().GetHashTreeRoot(options.Preset)))
            {
                logger.LogWarning("Finalized header in update is empty");
                return false;
            }
        }
        else
        {
            byte[] finalizedRoot;
            
            if (updateFinalizedSlot == 0)
            {
                if (!update.FinalizedHeader.GetHashTreeRoot(options.Preset).SequenceEqual(DenebLightClientHeader.CreateDefault().GetHashTreeRoot(options.Preset)))
                {
                    logger.LogWarning("Finalized header in update is empty");
                    return false; 
                }

                finalizedRoot = new byte[Constants.RootLength];
            }
            else
            {
                if (!DenebHelpers.IsValidLightClientHeader(update.FinalizedHeader, options.Preset))
                {
                    logger.LogWarning("Invalid finalized header in update");
                    return false;
                }
                
                finalizedRoot = update.FinalizedHeader.Beacon.GetHashTreeRoot(options.Preset);
            }
            
            var leaf = finalizedRoot;
            var branch = update.FinalityBranch;
            var depth = Constants.FinalityBranchDepth;
            var index = (int)AltairHelpers.GetSubtreeIndex(Constants.FinalizedRootGIndex);
            var root = update.AttestedHeader.Beacon.StateRoot;
                
            if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root))
            {
                logger.LogWarning("Invalid finality branch in update");
                return false;
            }
        }

        if (!DenebHelpers.IsSyncCommitteeUpdate(update))
        {
            if (!update.NextSyncCommittee.Equals(AltairSyncCommittee.CreateDefault()))
            {
                logger.LogWarning("Next sync committee in update is not empty");
                return false;
            }
        }
        else
        {
            if (updateAttestedPeriod == storePeriod && DenebHelpers.IsNextSyncCommitteeKnown(store))
            {
                if (!update.NextSyncCommittee.Equals(store.NextSyncCommittee))
                {
                    logger.LogWarning("Next sync committee in update does not match store");
                    return false;
                }
            }

            var leaf = update.NextSyncCommittee.GetHashTreeRoot(options.Preset);
            var branch = update.NextSyncCommitteeBranch;
            var depth = Constants.NextSyncCommitteeBranchDepth;
            var index = (int)AltairHelpers.GetSubtreeIndex(Constants.NextSyncCommitteeGIndex);
            var root = update.AttestedHeader.Beacon.StateRoot;
            
            if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root))
            {
                logger.LogWarning("Invalid next sync committee branch in update");
                return false;
            }
        }

        AltairSyncCommittee syncCommittee;
        
        if (updateSignaturePeriod == storePeriod)
        {
            syncCommittee = store.CurrentSyncCommittee;
        }
        else
        {
            syncCommittee = store.NextSyncCommittee;
        }

        var syncCommitteePubKeys = new byte[syncAggregate.SyncCommitteeBits.Count(b => b)][];
        var count = 0;
        
        for (var i = 0; i < syncAggregate.SyncCommitteeBits.Count; i++)
        {
            if (syncAggregate.SyncCommitteeBits[i])
            {
                syncCommitteePubKeys[count] = syncCommittee.PubKeys[i];
                count++;
            }
        }

        var forkVersionSlot = Math.Max(update.SignatureSlot, 1) - 1;
        var forkVersion = Phase0Helpers.ComputeForkVersion(Phase0Helpers.ComputeEpochAtSlot(forkVersionSlot));
        var domain = Phase0Helpers.ComputeDomain(DomainTypes.DomainSyncCommittee, forkVersion, options);
        var signingRoot = Phase0Helpers.ComputeSigningRoot(update.AttestedHeader.Beacon, domain, options.Preset);
        var blsPublicKeys = new PublicKey[syncCommitteePubKeys.Length];
        var message = new Msg();
        message.Set(signingRoot);
        
        for(var i = 0; i < syncCommitteePubKeys.Length; i++)
        {
            blsPublicKeys[i].Deserialize(syncCommitteePubKeys[i]);
        }
        
        var signature = new Signature();
        signature.Deserialize(syncAggregate.SyncCommitteeSignature);
        var result = signature.FastAggregateVerify(blsPublicKeys, message);

        if (!result)
        {
            logger.LogWarning("Invalid sync committee signature in update");
            return false;
        }
        
        return true;
    }

    public static bool ApplyLightClientUpdate(DenebLightClientStore store, DenebLightClientUpdate update, ILogger<SyncProtocol> logger)
    {
        var storePeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(store.FinalizedHeader.Beacon.Slot);
        var updateFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.FinalizedHeader.Beacon.Slot);

        if (!DenebHelpers.IsNextSyncCommitteeKnown(store))
        {
            if (updateFinalizedPeriod != storePeriod)
            {
                logger.LogWarning("Invalid finalized period in update");
                return false;
            }

            store.NextSyncCommittee = update.NextSyncCommittee;
        }
        else if (updateFinalizedPeriod == storePeriod + 1)
        {
            store.CurrentSyncCommittee = store.NextSyncCommittee;
            store.NextSyncCommittee = update.NextSyncCommittee;
            store.PreviousMaxActiveParticipants = store.CurrentMaxActiveParticipants;
            store.CurrentMaxActiveParticipants = 0;
        }

        if (update.FinalizedHeader.Beacon.Slot > store.FinalizedHeader.Beacon.Slot)
        {
            store.FinalizedHeader = update.FinalizedHeader;

            if (store.FinalizedHeader.Beacon.Slot > store.OptimisticHeader.Beacon.Slot)
            {
                store.OptimisticHeader = store.FinalizedHeader;
            }
        }

        return true;
    }

    public static void ProcessLightClientStoreForceUpdate(DenebLightClientStore store, ulong currentSlot, ILogger<SyncProtocol> logger)
    {
        if (currentSlot > store.FinalizedHeader.Beacon.Slot + (ulong)AltairPreset.UpdateTimeout &&
            store.BestValidUpdate != null)
        {
            if (store.BestValidUpdate.FinalizedHeader.Beacon.Slot <= store.FinalizedHeader.Beacon.Slot)
            {
                store.BestValidUpdate.FinalizedHeader = store.BestValidUpdate.AttestedHeader;
            }
            
            ApplyLightClientUpdate(store, store.BestValidUpdate, logger);
            store.BestValidUpdate = null;
        }
    }

    public static bool ProcessLightClientUpdate(DenebLightClientStore store, DenebLightClientUpdate update,
        ulong currentSlot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        var result = ValidateLightClientUpdate(store, update, currentSlot, options, logger);
        
        var syncCommitteeBits = update.SyncAggregate.SyncCommitteeBits;
        
        if(store.BestValidUpdate == null || DenebHelpers.IsBetterUpdate(update, store.BestValidUpdate))
        {
            store.BestValidUpdate = update;
        }
        
        store.CurrentMaxActiveParticipants = Math.Max(store.CurrentMaxActiveParticipants, (ulong)syncCommitteeBits.Count(b => b));
        
        if((ulong)syncCommitteeBits.Count(b => b) > DenebHelpers.GetSafetyThreshold(store) && update.AttestedHeader.Beacon.Slot > store.OptimisticHeader.Beacon.Slot)
        {
            store.OptimisticHeader = update.AttestedHeader;
        }
        
        var updateHasFinalizedNextSyncCommittee = !DenebHelpers.IsNextSyncCommitteeKnown(store)
                                                                 && DenebHelpers.IsSyncCommitteeUpdate(update) && DenebHelpers.IsFinalityUpdate(update)
                                                                 && AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.FinalizedHeader.Beacon.Slot)
                                                                     == AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.AttestedHeader.Beacon.Slot);

        if ((ulong)syncCommitteeBits.Count(b => b) * 3 >= (ulong)(syncCommitteeBits.Count * 2) && update.FinalizedHeader.Beacon.Slot > store.FinalizedHeader.Beacon.Slot || updateHasFinalizedNextSyncCommittee)
        {
            result = ApplyLightClientUpdate(store, update, logger);
            store.BestValidUpdate = null;
        }

        return result;
    }

    public static bool ProcessLightClientFinalityUpdate(DenebLightClientStore store,
        DenebLightClientFinalityUpdate finalityUpdate, ulong currentSlot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        var nextSyncCommitteeBranch = new byte[Constants.NextSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < nextSyncCommitteeBranch.Length; i++)
        {
            nextSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        var update = DenebLightClientUpdate.CreateFrom(
            finalityUpdate.AttestedHeader,
            AltairSyncCommittee.CreateDefault(),
            nextSyncCommitteeBranch,
            finalityUpdate.FinalizedHeader,
            finalityUpdate.FinalityBranch,
            finalityUpdate.SyncAggregate,
            finalityUpdate.SignatureSlot);
        
        return ProcessLightClientUpdate(store, update, currentSlot, options, logger);
    }

    public static bool ProcessLightClientOptimisticUpdate(DenebLightClientStore store,
        DenebLightClientOptimisticUpdate optimisticUpdate, ulong currentSlot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        var nextSyncCommitteeBranch = new byte[Constants.NextSyncCommitteeBranchDepth][];
        var finalityBranch = new byte[Constants.FinalityBranchDepth][];
        
        for (var i = 0; i < nextSyncCommitteeBranch.Length; i++)
        {
            nextSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        for (var i = 0; i < finalityBranch.Length; i++)
        {
            finalityBranch[i] = new byte[Bytes32.Length];
        }
        
        var update = DenebLightClientUpdate.CreateFrom(
            optimisticUpdate.AttestedHeader,
            AltairSyncCommittee.CreateDefault(), 
            nextSyncCommitteeBranch,
            DenebLightClientHeader.CreateDefault(), 
            finalityBranch,
            optimisticUpdate.SyncAggregate,
            optimisticUpdate.SignatureSlot);

        return ProcessLightClientUpdate(store, update, currentSlot, options, logger);
    }
}