using Cortex.Containers;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Microsoft.Extensions.Logging;
using Planetarium.Cryptography.BLS12_381;

namespace Lantern.Beacon.Sync.Processors;

public static class CapellaProcessors
{
    public static void ValidateLightClientUpdate(CapellaLightClientStore store, CapellaLightClientUpdate update, ulong currentSlot, byte[] genesisValidatorsRoot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        var syncAggregate = update.SyncAggregate;

        if (!(syncAggregate.SyncCommitteeBits.Count(b => b) >= AltairPreset.MinSyncCommitteeParticipants))
        {
            logger.LogWarning("Sync aggregate has insufficient active participants in update");
            return;
        }

        if (!CapellaHelpers.IsValidLightClientHeader(update.AttestedHeader, options.Preset))
        {
            logger.LogWarning("Invalid attested header in update");
            return;
        }

        var updateAttestedSlot = update.AttestedHeader.Beacon.Slot;
        var updateFinalizedSlot = update.FinalizedHeader.Beacon.Slot;

        if (!(currentSlot >= update.SignatureSlot && update.SignatureSlot > updateAttestedSlot && updateAttestedSlot >= updateFinalizedSlot))
        {
            logger.LogWarning("Invalid slot values in update");
            return;
        }

        var storePeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(store.FinalizedHeader.Beacon.Slot);
        var updateSignaturePeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.SignatureSlot);

        if (CapellaHelpers.IsNextSyncCommitteeKnown(store))
        {
            if (!(updateSignaturePeriod == storePeriod || updateSignaturePeriod == storePeriod + 1))
            {
                logger.LogWarning("Invalid sync committee period in update");
                return;
            }
        }
        else
        {
            if (updateSignaturePeriod != storePeriod)
            {
                logger.LogWarning("Invalid sync committee period in update");
                return;
            }
        }

        var updateAttestedPeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(updateAttestedSlot);
        var updateHasNextSyncCommittee = !CapellaHelpers.IsNextSyncCommitteeKnown(store) && 
                                         CapellaHelpers.IsSyncCommitteeUpdate(update) && updateAttestedPeriod == storePeriod;

        if (!(updateAttestedSlot > store.FinalizedHeader.Beacon.Slot || updateHasNextSyncCommittee))
        {
            logger.LogWarning("Update is older than finalised slot");
            return;
        }

        if (!CapellaHelpers.IsFinalityUpdate(update))
        {
            if (!update.FinalizedHeader.Equals(CapellaLightClientHeader.CreateDefault()))
            {
                logger.LogWarning("Finalized header in update is empty");
                return;
            }
        }
        else
        {
            byte[] finalizedRoot;
            
            if (updateFinalizedSlot == 0)
            {
                if (!update.FinalizedHeader.Equals(CapellaLightClientHeader.CreateDefault()))
                {
                    logger.LogWarning("Finalized header in update is empty");
                    return;
                }

                finalizedRoot = new byte[Constants.RootLength];
            }
            else
            {
                if (!CapellaHelpers.IsValidLightClientHeader(update.FinalizedHeader, options.Preset))
                {
                    logger.LogWarning("Invalid finalized header in update");
                    return;
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
                return;
            }
        }

        if (!CapellaHelpers.IsSyncCommitteeUpdate(update))
        {
            if (!update.NextSyncCommittee.Equals(AltairSyncCommittee.CreateDefault()))
            {
                logger.LogWarning("Next sync committee in update is not empty");
                return;
            }
        }
        else
        {
            if (updateAttestedPeriod == storePeriod && CapellaHelpers.IsNextSyncCommitteeKnown(store))
            {
                if (!update.NextSyncCommittee.Equals(store.NextSyncCommittee))
                {
                    logger.LogWarning("Next sync committee in update does not match store");
                    return;
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
                return;
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
        var domain = Phase0Helpers.ComputeDomain(DomainTypes.DomainSyncCommittee, forkVersion, genesisValidatorsRoot, options.Preset);
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
        }
    }

    public static void ApplyLightClientUpdate(CapellaLightClientStore store, CapellaLightClientUpdate update, ILogger<SyncProtocol> logger)
    {
        var storePeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(store.FinalizedHeader.Beacon.Slot);
        var updateFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.FinalizedHeader.Beacon.Slot);

        if (!CapellaHelpers.IsNextSyncCommitteeKnown(store))
        {
            if (updateFinalizedPeriod != storePeriod)
            {
                logger.LogWarning("Invalid finalized period in update");
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
            Console.WriteLine("Applying finalized header update");
            store.FinalizedHeader = update.FinalizedHeader;

            if (store.FinalizedHeader.Beacon.Slot > store.OptimisticHeader.Beacon.Slot)
            {
                store.OptimisticHeader = store.FinalizedHeader;
            }
        }
    }

    public static void ProcessLightClientStoreForceUpdate(CapellaLightClientStore store, ulong currentSlot, ILogger<SyncProtocol> logger)
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

    public static void ProcessLightClientUpdate(CapellaLightClientStore store, CapellaLightClientUpdate update,
        ulong currentSlot, byte[] genesisValidatorsRoot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        ValidateLightClientUpdate(store, update, currentSlot, genesisValidatorsRoot, options, logger);
        var syncCommitteeBits = update.SyncAggregate.SyncCommitteeBits;
        
        if(store.BestValidUpdate == null || CapellaHelpers.IsBetterUpdate(update, store.BestValidUpdate))
        {
            store.BestValidUpdate = update;
        }
        
        store.CurrentMaxActiveParticipants = Math.Max(store.CurrentMaxActiveParticipants, (ulong)syncCommitteeBits.Count(b => b));
        
        if((ulong)syncCommitteeBits.Count(b => b) > CapellaHelpers.GetSafetyThreshold(store) && update.AttestedHeader.Beacon.Slot > store.OptimisticHeader.Beacon.Slot)
        {
            store.OptimisticHeader = update.AttestedHeader;
        }
        
        var updateHasFinalizedNextSyncCommittee = !CapellaHelpers.IsNextSyncCommitteeKnown(store)
                                                                 && CapellaHelpers.IsSyncCommitteeUpdate(update) && CapellaHelpers.IsFinalityUpdate(update)
                                                                 && AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.FinalizedHeader.Beacon.Slot)
                                                                     == AltairHelpers.ComputeSyncCommitteePeriodAtSlot(update.AttestedHeader.Beacon.Slot);

        if ((ulong)syncCommitteeBits.Count(b => b) * 3 >= (ulong)(syncCommitteeBits.Count * 2) && update.FinalizedHeader.Beacon.Slot > store.FinalizedHeader.Beacon.Slot || updateHasFinalizedNextSyncCommittee)
        {
            ApplyLightClientUpdate(store, update, logger);
            store.BestValidUpdate = null;
        }
    }

    public static void ProcessLightClientFinalityUpdate(CapellaLightClientStore store,
        CapellaLightClientFinalityUpdate finalityUpdate, ulong currentSlot, byte[] genesisValidatorsRoot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
    {
        var nextSyncCommitteeBranch = new byte[Constants.NextSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < nextSyncCommitteeBranch.Length; i++)
        {
            nextSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        var update = CapellaLightClientUpdate.CreateFrom(
            finalityUpdate.AttestedHeader,
            AltairSyncCommittee.CreateDefault(),
            nextSyncCommitteeBranch,
            finalityUpdate.FinalizedHeader,
            finalityUpdate.FinalityBranch,
            finalityUpdate.SyncAggregate,
            finalityUpdate.SignatureSlot);
        
        ProcessLightClientUpdate(store, update, currentSlot, genesisValidatorsRoot, options, logger);
    }

    public static void ProcessLightClientOptimisticUpdate(CapellaLightClientStore store,
        CapellaLightClientOptimisticUpdate optimisticUpdate, ulong currentSlot, byte[] genesisValidatorsRoot, SyncProtocolOptions options, ILogger<SyncProtocol> logger)
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
        
        var update = CapellaLightClientUpdate.CreateFrom(
            optimisticUpdate.AttestedHeader,
            AltairSyncCommittee.CreateDefault(), 
            nextSyncCommitteeBranch,
            CapellaLightClientHeader.CreateDefault(), 
            finalityBranch,
            optimisticUpdate.SyncAggregate,
            optimisticUpdate.SignatureSlot);
        
        ProcessLightClientUpdate(store, update, currentSlot, genesisValidatorsRoot, options, logger);
    }
}