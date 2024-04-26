using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types;
using Lantern.Beacon.SyncProtocol.Types.Altair;
using Lantern.Beacon.SyncProtocol.Types.Bellatrix;
using Lantern.Beacon.SyncProtocol.Types.Capella;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.SimpleSerialize;

public static class Serializers
{
    public static byte[] Encode(Phase0BeaconBlockHeader header)
    {
        var result = new byte[Phase0BeaconBlockHeader.BytesLength];

        Ssz.Encode(result.AsSpan(0, sizeof(ulong)), (ulong)header.Slot);
        Ssz.Encode(result.AsSpan(sizeof(ulong), sizeof(ulong)), (ulong)header.ProposerIndex);
        Ssz.Encode(result.AsSpan(2 * sizeof(ulong), Bytes32.Length), (ReadOnlySpan<byte>)header.ParentRoot);
        Ssz.Encode(result.AsSpan(2 * sizeof(ulong) + Bytes32.Length, Bytes32.Length), (ReadOnlySpan<byte>)header.StateRoot);
        Ssz.Encode(result.AsSpan(2 * sizeof(ulong) + 2 * Bytes32.Length, Bytes32.Length), (ReadOnlySpan<byte>)header.BodyRoot);

        return result;
    }
    
    public static byte[] Encode(AltairLightClientHeader header)
    {
        return Encode(header.Beacon);
    }
    
    public static byte[] Encode(AltairLightClientUpdate update)
    {
        var result = new byte[AltairLightClientUpdate.BytesLength];
        var attestedHeaderBytes = Encode(update.AttestedHeader);
        var nextSyncCommitteeBytes = Encode(update.NextAltairSyncCommittee);
        var finalizedHeaderBytes = Encode(update.FinalizedHeader);
        var syncAggregateBytes = Encode(update.AltairSyncAggregate);
           
        Array.Copy(attestedHeaderBytes, 0, result, 0, AltairLightClientHeader.BytesLength);
        Array.Copy(nextSyncCommitteeBytes, 0, result, AltairLightClientHeader.BytesLength, AltairSyncCommittee.BytesLength);
            
        var offset = AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength;
            
        foreach (var array in update.NextSyncCommitteeBranch)
        {
            Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
            offset += Bytes32.Length;
        }
            
        Array.Copy(finalizedHeaderBytes, 0, result, offset, AltairLightClientHeader.BytesLength);
            
        offset += AltairLightClientHeader.BytesLength;
            
        foreach (var array in update.FinalizedBranch)
        {
            Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
            offset += Bytes32.Length;
        }
            
        Array.Copy(syncAggregateBytes, 0, result, offset, AltairSyncAggregate.BytesLength);
            
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), (ulong)update.SignatureSlot);
            
        return result;
    }
    
    public static byte[] Encode(AltairLightClientOptimisticUpdate optimisticUpdate)
    {
        var result = new byte[AltairLightClientOptimisticUpdate.BytesLength];
        var attestedHeaderBytes = Encode(optimisticUpdate.AttestedHeader);
        var syncAggregateBytes = Encode(optimisticUpdate.AltairSyncAggregate);
            
        Array.Copy(attestedHeaderBytes, 0, result, 0, AltairLightClientHeader.BytesLength);
        Array.Copy(syncAggregateBytes, 0, result, AltairLightClientHeader.BytesLength, AltairSyncAggregate.BytesLength);
            
        Ssz.Encode(result.AsSpan(AltairLightClientHeader.BytesLength + AltairSyncAggregate.BytesLength, sizeof(ulong)), (ulong)optimisticUpdate.SignatureSlot);
            
        return result;
    }
    
    public static byte[] Encode(AltairLightClientFinalityUpdate finalityUpdate)
    {
        var result = new byte[AltairLightClientFinalityUpdate.BytesLength];
        var attestedHeaderBytes = Encode(finalityUpdate.AttestedHeader);
        var finalizedHeaderBytes = Encode(finalityUpdate.FinalizedHeader);
        var syncAggregateBytes = Encode(finalityUpdate.AltairSyncAggregate);
            
        Array.Copy(attestedHeaderBytes, 0, result, 0, AltairLightClientHeader.BytesLength);
        Array.Copy(finalizedHeaderBytes, 0, result, AltairLightClientHeader.BytesLength, AltairLightClientHeader.BytesLength);

        var offset = AltairLightClientHeader.BytesLength * 2;
            
        foreach (var array in finalityUpdate.FinalityBranch)
        {
            Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
            offset += Bytes32.Length;
        }
            
        Array.Copy(syncAggregateBytes, 0, result, offset, AltairSyncAggregate.BytesLength);
            
        Ssz.Encode(result.AsSpan(offset + AltairSyncAggregate.BytesLength, sizeof(ulong)), (ulong)finalityUpdate.SignatureSlot);
            
        return result;
    }
    
    public static byte[] Encode(AltairLightClientBootstrap bootstrap)
    {
        var result = new byte[AltairLightClientBootstrap.BytesLength];
        var headerBytes = Encode(bootstrap.Header);
        var syncCommitteeBytes = Encode(bootstrap.CurrentAltairSyncCommittee);
  
        Array.Copy(headerBytes, 0, result, 0, AltairLightClientHeader.BytesLength);
        Array.Copy(syncCommitteeBytes, 0, result, AltairLightClientHeader.BytesLength, AltairSyncCommittee.BytesLength);
            
        var offset = AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength;
            
        foreach (var array in bootstrap.CurrentSyncCommitteeBranch)
        {
            Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
            offset += Bytes32.Length;
        }
            
        return result;
    }
    
    public static byte[] Encode(AltairSyncAggregate altairSyncAggregate)
    {
        var result = new byte[AltairSyncAggregate.BytesLength];
            
        Ssz.EncodeVector(result.AsSpan(0, Constants.SyncCommitteeSize), altairSyncAggregate.SyncCommitteeBits);
        Ssz.Encode(result.AsSpan(Constants.SyncCommitteeSize, BlsSignature.Length), (ReadOnlySpan<byte>)altairSyncAggregate.SyncCommitteeSignature);
            
        return result;
    }
    
    public static byte[] Encode(AltairSyncCommittee altairSyncCommittee)
    {
        var result = new byte[AltairSyncCommittee.BytesLength];
        var offset = 0;
            
        foreach (var pubKey in altairSyncCommittee.PubKeys)
        {
            Ssz.Encode(result.AsSpan(offset, BlsPublicKey.Length), pubKey.AsSpan());
            offset += BlsPublicKey.Length;
        }
            
        Ssz.Encode(result.AsSpan(offset, BlsPublicKey.Length), altairSyncCommittee.AggregatePubKey.AsSpan());

        return result;
    }
    
        public static byte[] Encode(BellatrixExecutionPayloadHeader header)
    {
        var result = new byte[BellatrixExecutionPayloadHeader.BytesLength + header.ExtraData.Count];
        var offset = 0;
        
        Ssz.Encode(result.AsSpan(offset, Hash32.Length), (ReadOnlySpan<byte>)header.ParentHash);
        offset += Hash32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes20.Length), (ReadOnlySpan<byte>)header.FeeRecipientAddress);
        offset += Bytes20.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.StateRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.ReceiptsRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Constants.BytesPerLogsBloom), header.LogsBloom);
        offset += Constants.BytesPerLogsBloom;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.PrevRandoa);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.BlockNumber);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.GasLimit);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.GasUsed);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.Timestamp);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, Constants.BytesPerLengthOffset), result.Length - header.ExtraData.Count);
        offset += Constants.BytesPerLengthOffset;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), header.BaseFeePerGas);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Hash32.Length), (ReadOnlySpan<byte>)header.BlockHash);
        offset += Hash32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.TransactionsRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, header.ExtraData.Count), header.ExtraData.ToArray());
        return result;
    }
        
    public static byte[] Encode(CapellaExecutionPayloadHeader header)
    {
        var result = new byte[CapellaExecutionPayloadHeader.BytesLength + header.ExtraData.Count];
        var offset = 0;
        
        Ssz.Encode(result.AsSpan(offset, Hash32.Length), (ReadOnlySpan<byte>)header.ParentHash);
        offset += Hash32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes20.Length), (ReadOnlySpan<byte>)header.FeeRecipientAddress);
        offset += Bytes20.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.StateRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.ReceiptsRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Constants.BytesPerLogsBloom), header.LogsBloom);
        offset += Constants.BytesPerLogsBloom;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.PrevRandoa);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.BlockNumber);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.GasLimit);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.GasUsed);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, sizeof(ulong)), header.Timestamp);
        offset += sizeof(ulong);
        
        Ssz.Encode(result.AsSpan(offset, Constants.BytesPerLengthOffset), result.Length - header.ExtraData.Count);
        offset += Constants.BytesPerLengthOffset;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), header.BaseFeePerGas);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Hash32.Length), (ReadOnlySpan<byte>)header.BlockHash);
        offset += Hash32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.TransactionsRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)header.WithdrawalsRoot);
        offset += Bytes32.Length;
        
        Ssz.Encode(result.AsSpan(offset, header.ExtraData.Count), header.ExtraData.ToArray());
        
        return result;
    }   
    
    public static byte[] Encode(CapellaLightClientHeader header)
    {
        var result = new byte[CapellaLightClientHeader.BytesLength + header.BellatrixExecution.ExtraData.Count];
        var beaconBytes = Encode(header.Beacon);
        var offset = 0;
            
        Array.Copy(beaconBytes, 0, result, 0, Phase0BeaconBlockHeader.BytesLength);
        offset += Phase0BeaconBlockHeader.BytesLength;
            
        Ssz.Encode(result.AsSpan(offset, Constants.BytesPerLengthOffset), Constants.ExecutionBranchDepth * Bytes32.Length + offset + Constants.BytesPerLengthOffset);
        offset += Constants.BytesPerLengthOffset;
            
        foreach (var array in header.ExecutionBranch)
        {
            Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
            offset += Bytes32.Length;
        }

        var executionBytes = Encode(header.BellatrixExecution);
        Array.Copy(executionBytes, 0, result, offset, executionBytes.Length);
            
        return result;
    }
}