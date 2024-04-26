using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types;
using Lantern.Beacon.SyncProtocol.Types.Altair;
using Lantern.Beacon.SyncProtocol.Types.Bellatrix;
using Lantern.Beacon.SyncProtocol.Types.Capella;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.SimpleSerialize;

public static class Deserializers
{
    public static Phase0BeaconBlockHeader DecodePhase0BeaconBlockHeader(byte[] bytes)
    {
        var slot = new Slot(Ssz.DecodeULong(bytes.AsSpan(0, sizeof(ulong))));
        var proposerIndex= new ValidatorIndex(Ssz.DecodeULong(bytes.AsSpan(sizeof(ulong), sizeof(ulong))));
        var parentRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(2 * sizeof(ulong), Bytes32.Length)).AsSpan());
        var stateRoot =
            new Bytes32(
                Ssz.DecodeBytes32(bytes.AsSpan(2 * sizeof(ulong) + Bytes32.Length, Bytes32.Length)).AsSpan());
        var bodyRoot = new Bytes32(
            Ssz.DecodeBytes32(bytes.AsSpan(2 * sizeof(ulong) + 2 * Bytes32.Length, Bytes32.Length)).AsSpan());
        
        return new Phase0BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
    }

    public static Phase0BeaconBlockHeader DecodePhase0BeaconBlockHeader(Span<byte> bytes)
    {
        var slot = new Slot(Ssz.DecodeULong(bytes[..sizeof(ulong)]));
        var proposerIndex = new ValidatorIndex(Ssz.DecodeULong(bytes.Slice(sizeof(ulong), sizeof(ulong))));
        var parentRoot = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(2 * sizeof(ulong), Bytes32.Length)).AsSpan());
        var stateRoot = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(2 * sizeof(ulong) + Bytes32.Length, Bytes32.Length)).AsSpan());
        var bodyRoot = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(2 * sizeof(ulong) + 2 * Bytes32.Length, Bytes32.Length)).AsSpan());

        return new Phase0BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
    }
    
    public static AltairLightClientHeader DecodeAltairLightClientHeader(byte[] bytes)
    {
        return new AltairLightClientHeader(DecodePhase0BeaconBlockHeader(bytes));
    }
        
    public static AltairLightClientHeader DecodeAltairLightClientHeader(Span<byte> bytes)
    {
        return new AltairLightClientHeader(DecodePhase0BeaconBlockHeader(bytes));
    }
    
    public static AltairLightClientUpdate DecodeAltairLightClientUpdate(byte[] bytes)
    {
        var attestedHeader = DecodeAltairLightClientHeader(bytes.AsSpan(0, AltairLightClientHeader.BytesLength));
        var nextSyncCommittee = DecodeAltairSyncCommittee(bytes.AsSpan(AltairLightClientHeader.BytesLength, AltairSyncCommittee.BytesLength));
        var nextSyncCommitteeBranch = new Bytes32[Constants.NextSyncCommitteeBranchDepth];
        var offset = AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength;
            
        for (var i = 0; i < Constants.NextSyncCommitteeBranchDepth; i++)
        {
            nextSyncCommitteeBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
            offset += Bytes32.Length;
        }
            
        var finalizedHeader = DecodeAltairLightClientHeader(bytes.AsSpan(offset, AltairLightClientHeader.BytesLength));
        var finalizedBranch = new Bytes32[Constants.FinalityBranchDepth];
        offset += AltairLightClientHeader.BytesLength;
            
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalizedBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
            offset += Bytes32.Length;
        }
            
        var syncAggregate = DecodeAltairSyncAggregate(bytes.AsSpan(offset, AltairSyncAggregate.BytesLength));
        offset += AltairSyncAggregate.BytesLength;
            
        var signatureSlot = new Slot(Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong))));
            
        return new AltairLightClientUpdate(attestedHeader, nextSyncCommittee, nextSyncCommitteeBranch, finalizedHeader!, finalizedBranch, syncAggregate, signatureSlot);
    }
    
    public static AltairLightClientUpdate DecodeAltairLightClientUpdate(Span<byte> bytes)
    {
        var attestedHeader = DecodeAltairLightClientHeader(bytes[..AltairLightClientHeader.BytesLength]);
        var nextSyncCommittee = DecodeAltairSyncCommittee((bytes.Slice(AltairLightClientHeader.BytesLength, AltairSyncCommittee.BytesLength)));
        var nextSyncCommitteeBranch = new Bytes32[Constants.NextSyncCommitteeBranchDepth];
        var offset = AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength;
            
        for (var i = 0; i < Constants.NextSyncCommitteeBranchDepth; i++)
        {
            nextSyncCommitteeBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(offset, Bytes32.Length)).AsSpan());
            offset += Bytes32.Length;
        }
            
        var finalizedHeader = DecodeAltairLightClientHeader(bytes.Slice(offset, AltairLightClientHeader.BytesLength));
        var finalizedBranch = new Bytes32[Constants.FinalityBranchDepth];
        offset += AltairLightClientHeader.BytesLength;
            
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalizedBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(offset, Bytes32.Length)).AsSpan());
            offset += Bytes32.Length;
        }
            
        var syncAggregate = DecodeAltairSyncAggregate(bytes.Slice(offset, AltairSyncAggregate.BytesLength));
        offset += AltairSyncAggregate.BytesLength;
            
        var signatureSlot = new Slot(Ssz.DecodeULong(bytes.Slice(offset, sizeof(ulong))));
            
        return new AltairLightClientUpdate(attestedHeader, nextSyncCommittee, nextSyncCommitteeBranch, finalizedHeader!, finalizedBranch, syncAggregate, signatureSlot);
    }
    
    public static AltairLightClientOptimisticUpdate DecodeAltairLightClientOptimisticUpdate(byte[] bytes)
    {
        var attestedHeader = DecodeAltairLightClientHeader(bytes.AsSpan(0, AltairLightClientHeader.BytesLength));
        var syncAggregate = DecodeAltairSyncAggregate(bytes.AsSpan(AltairLightClientHeader.BytesLength, AltairSyncAggregate.BytesLength));
        var signatureSlot = Ssz.DecodeULong(bytes.AsSpan(AltairLightClientHeader.BytesLength + AltairSyncAggregate.BytesLength, sizeof(ulong)));
            
        return new AltairLightClientOptimisticUpdate(attestedHeader, syncAggregate, new Slot(signatureSlot));
    }
    
    public static AltairLightClientFinalityUpdate DecodeAltairLightClientFinalityUpdate(byte[] bytes)
    {
        var attestedHeader = DecodeAltairLightClientHeader(bytes.AsSpan(0, AltairLightClientHeader.BytesLength));
        var finalizedHeader = DecodeAltairLightClientHeader(bytes.AsSpan(AltairLightClientHeader.BytesLength, AltairLightClientHeader.BytesLength));
        var finalityBranch = new Bytes32[Constants.FinalityBranchDepth];
        var offset = 2 * AltairLightClientHeader.BytesLength;
            
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalityBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
            offset += Bytes32.Length;
        }
            
        var syncAggregate = DecodeAltairSyncAggregate(bytes.AsSpan(offset, AltairSyncAggregate.BytesLength));
        var signatureSlot = Ssz.DecodeULong(bytes.AsSpan(offset + AltairSyncAggregate.BytesLength, sizeof(ulong)));
            
        return new AltairLightClientFinalityUpdate(attestedHeader, finalizedHeader, finalityBranch, syncAggregate, new Slot(signatureSlot));
    }
    
    public static AltairLightClientBootstrap DecodeAltairLightClientBootstrap(byte[] bytes)
    {
        var header = DecodeAltairLightClientHeader(bytes.AsSpan(0, AltairLightClientHeader.BytesLength));
        var syncCommittee = DecodeAltairSyncCommittee((bytes.AsSpan(AltairLightClientHeader.BytesLength, AltairSyncCommittee.BytesLength)));
        var branch = new Bytes32[Constants.CurrentSyncCommitteeBranchDepth];
        var offset = AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength;
            
        for (var i = 0; i < Constants.CurrentSyncCommitteeBranchDepth; i++)
        {
            branch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
            offset += Bytes32.Length;
        }
            
        return new AltairLightClientBootstrap(header, syncCommittee, branch);
    }
    
    public static AltairSyncAggregate DecodeAltairSyncAggregate(byte[] bytes)
    {
        var syncCommitteeBits = Ssz.DecodeBitvector(bytes.AsSpan(0, Constants.SyncCommitteeSize), Constants.SyncCommitteeSize);
        var syncCommitteeSignature = new BlsSignature(Ssz.DecodeBytes(bytes.AsSpan(Constants.SyncCommitteeSize, BlsSignature.Length)));
            
        return new AltairSyncAggregate(syncCommitteeBits, syncCommitteeSignature);
    }
        
    public static AltairSyncAggregate DecodeAltairSyncAggregate(Span<byte> bytes)
    {
        var syncCommitteeBits = Ssz.DecodeBitvector(bytes[..Constants.SyncCommitteeSize], Constants.SyncCommitteeSize);
        var syncCommitteeSignature = new BlsSignature(Ssz.DecodeBytes(bytes.Slice(Constants.SyncCommitteeSize, BlsSignature.Length)));
            
        return new AltairSyncAggregate(syncCommitteeBits, syncCommitteeSignature);
    }
    
    public static AltairSyncCommittee DecodeAltairSyncCommittee(byte[] bytes)
    {
        var offset = 0;
        var pubKeys = new BlsPublicKey[Constants.SyncCommitteeSize];
            
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            pubKeys[i] = new BlsPublicKey(bytes.AsSpan(offset, BlsPublicKey.Length));
            offset += BlsPublicKey.Length;
        }
            
        var aggregatePubKey = new BlsPublicKey(bytes.AsSpan(offset, BlsPublicKey.Length));
            
        return new AltairSyncCommittee(pubKeys, aggregatePubKey);
    }
        
    public static AltairSyncCommittee DecodeAltairSyncCommittee(Span<byte> bytes)
    {
        var offset = 0;
        var pubKeys = new BlsPublicKey[Constants.SyncCommitteeSize];
            
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            pubKeys[i] = new BlsPublicKey(bytes.Slice(offset, BlsPublicKey.Length));
            offset += BlsPublicKey.Length;
        }
            
        var aggregatePubKey = new BlsPublicKey(bytes.Slice(offset, BlsPublicKey.Length));
            
        return new AltairSyncCommittee(pubKeys, aggregatePubKey);
    }
    
    public static BellatrixExecutionPayloadHeader DecodeBellatrixExecutionPayloadHeader(byte[] bytes)
    {
        var offset = 0;
        var parentHash = new Hash32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Hash32.Length)).AsSpan());
        offset += Hash32.Length;
        
        var address = new Bytes20(Ssz.DecodeBytes(bytes.AsSpan(offset, Bytes20.Length)));
        offset += Bytes20.Length;
        
        var stateRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var receiptsRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var logsBloom = bytes.AsSpan(offset, Constants.BytesPerLogsBloom).ToArray();
        offset += Constants.BytesPerLogsBloom;
        
        var prevRandao = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var blockNumber = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var gasLimit = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var gasUsed = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var timestamp = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var extraDataOffset = BitConverter.ToInt32(bytes.AsSpan(offset, Constants.BytesPerLengthOffset));
        offset += sizeof(uint);
        
        var baseFeePerGas = Ssz.DecodeUInt256(bytes.AsSpan(offset, Bytes32.Length));
        offset += Bytes32.Length;
        
        var blockHash = new Hash32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Hash32.Length)).AsSpan());
        offset += Hash32.Length;
        
        var transactionsRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        var extraDataBytes = Ssz.DecodeBytes(bytes.AsSpan(extraDataOffset, bytes.Length - extraDataOffset));
        var extraData = new List<byte>(Constants.MaxExtraDataBytes);
        extraData.AddRange(extraDataBytes);
        
        return new BellatrixExecutionPayloadHeader(parentHash, address, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot);
    }

    public static BellatrixExecutionPayloadHeader DecodeBellatrixExecutionPayloadHeader(Span<byte> bytes)
    {
        var offset = 0;
        var parentHash = new Hash32(Ssz.DecodeBytes32(bytes[..Hash32.Length]).AsSpan());
        offset += Hash32.Length;
        
        var address = new Bytes20(Ssz.DecodeBytes(bytes[offset..(offset + Bytes20.Length)]));
        offset += Bytes20.Length;
        
        var stateRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var receiptsRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var logsBloom = bytes[offset..(offset + Constants.BytesPerLogsBloom)].ToArray();
        offset += Constants.BytesPerLogsBloom;
        
        var prevRandao = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var blockNumber = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var gasLimit = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var gasUsed = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var timestamp = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var extraDataOffset = BitConverter.ToInt32(bytes[offset..(offset + Constants.BytesPerLengthOffset)]);
        offset += sizeof(uint);
        
        var baseFeePerGas = Ssz.DecodeUInt256(bytes[offset..(offset + Bytes32.Length)]);
        offset += Bytes32.Length;
        
        var blockHash = new Hash32(Ssz.DecodeBytes32(bytes[offset..(offset + Hash32.Length)]).AsSpan());
        offset += Hash32.Length;
        
        var transactionsRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        var extraDataBytes = Ssz.DecodeBytes(bytes.Slice(extraDataOffset, bytes.Length - extraDataOffset));
        var extraData = new List<byte>(Constants.MaxExtraDataBytes);
        extraData.AddRange(extraDataBytes);
        
        return new BellatrixExecutionPayloadHeader(parentHash, address, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot);
    }
    
    public static CapellaExecutionPayloadHeader DecodeCapellaExecutionPayloadHeader(byte[] bytes)
    {
        var offset = 0;
        var parentHash = new Hash32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Hash32.Length)).AsSpan());
        offset += Hash32.Length;
        
        var address = new Bytes20(Ssz.DecodeBytes(bytes.AsSpan(offset, Bytes20.Length)));
        offset += Bytes20.Length;
        
        var stateRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var receiptsRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var logsBloom = bytes.AsSpan(offset, Constants.BytesPerLogsBloom).ToArray();
        offset += Constants.BytesPerLogsBloom;
        
        var prevRandao = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var blockNumber = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var gasLimit = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var gasUsed = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var timestamp = Ssz.DecodeULong(bytes.AsSpan(offset, sizeof(ulong)));
        offset += sizeof(ulong);
        
        var extraDataOffset = BitConverter.ToInt32(bytes.AsSpan(offset, Constants.BytesPerLengthOffset));
        offset += sizeof(uint);
        
        var baseFeePerGas = Ssz.DecodeUInt256(bytes.AsSpan(offset, Bytes32.Length));
        offset += Bytes32.Length;
        
        var blockHash = new Hash32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Hash32.Length)).AsSpan());
        offset += Hash32.Length;
        
        var transactionsRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var withdrawalsRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
        offset += Bytes32.Length;
        
        var extraDataBytes = Ssz.DecodeBytes(bytes.AsSpan(offset, bytes.Length - extraDataOffset));
        var extraData = new List<byte>(Constants.MaxExtraDataBytes);
        extraData.AddRange(extraDataBytes);
        
        return new CapellaExecutionPayloadHeader(parentHash, address, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot);
    }

    public static CapellaExecutionPayloadHeader DecodeCapellaExecutionPayloadHeader(Span<byte> bytes)
    {
        var offset = 0;
        var parentHash = new Hash32(Ssz.DecodeBytes32(bytes[..Hash32.Length]).AsSpan());
        offset += Hash32.Length;
        
        var address = new Bytes20(Ssz.DecodeBytes(bytes[offset..(offset + Bytes20.Length)]));
        offset += Bytes20.Length;
        
        var stateRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var receiptsRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var logsBloom = bytes[offset..(offset + Constants.BytesPerLogsBloom)].ToArray();
        offset += Constants.BytesPerLogsBloom;
        
        var prevRandao = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var blockNumber = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var gasLimit = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var gasUsed = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var timestamp = Ssz.DecodeULong(bytes[offset..(offset + sizeof(ulong))]);
        offset += sizeof(ulong);
        
        var extraDataOffset = BitConverter.ToInt32(bytes[offset..(offset + Constants.BytesPerLengthOffset)]);
        offset += sizeof(uint);
        
        var baseFeePerGas = Ssz.DecodeUInt256(bytes[offset..(offset + Bytes32.Length)]);
        offset += Bytes32.Length;
        
        var blockHash = new Hash32(Ssz.DecodeBytes32(bytes[offset..(offset + Hash32.Length)]).AsSpan());
        offset += Hash32.Length;
        
        var transactionsRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var withdrawalsRoot = new Bytes32(Ssz.DecodeBytes32(bytes[offset..(offset + Bytes32.Length)]).AsSpan());
        offset += Bytes32.Length;
        
        var extraDataBytes = Ssz.DecodeBytes(bytes.Slice(offset, bytes.Length - extraDataOffset));
        var extraData = new List<byte>(Constants.MaxExtraDataBytes);
        extraData.AddRange(extraDataBytes);
        
        return new CapellaExecutionPayloadHeader(parentHash, address, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot);
    }
    
    public static CapellaLightClientHeader DecodeCapellaLightClientHeader(byte[] bytes)
    {
        var offset = 0;
        var beacon = DecodePhase0BeaconBlockHeader(bytes.AsSpan(0, Phase0BeaconBlockHeader.BytesLength));
        offset += Phase0BeaconBlockHeader.BytesLength;
        
        var executionPayloadHeaderOffset = BitConverter.ToInt32(bytes.AsSpan(offset, Constants.BytesPerLengthOffset));
        offset += Constants.BytesPerLengthOffset;
        
        var executionBranch = new Bytes32[Constants.ExecutionBranchDepth];
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            executionBranch[i] = new Bytes32(bytes.AsSpan(offset, Bytes32.Length));
            offset += Bytes32.Length;
        }
        
        var execution = DecodeCapellaExecutionPayloadHeader(bytes.AsSpan(executionPayloadHeaderOffset,  bytes.Length - executionPayloadHeaderOffset));
        
        return new CapellaLightClientHeader(beacon, execution, executionBranch);
    }
        
    public static CapellaLightClientHeader DecodeCapellaLightClientHeader(Span<byte> bytes)
    {
        var offset = 0;
        var beacon = DecodePhase0BeaconBlockHeader(bytes[..Phase0BeaconBlockHeader.BytesLength]);
        offset += Phase0BeaconBlockHeader.BytesLength;
        
        var executionPayloadHeaderOffset = BitConverter.ToInt32(bytes.Slice(offset, Constants.BytesPerLengthOffset));
        offset += Constants.BytesPerLengthOffset;
        
        var executionBranch = new Bytes32[Constants.ExecutionBranchDepth];
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            executionBranch[i] = new Bytes32(bytes.Slice(offset, Bytes32.Length));
            offset += Bytes32.Length;
        }
        
        var execution = DecodeCapellaExecutionPayloadHeader(bytes.Slice(executionPayloadHeaderOffset,  bytes.Length - executionPayloadHeaderOffset));
        
        return new CapellaLightClientHeader(beacon, execution, executionBranch);
    }
}