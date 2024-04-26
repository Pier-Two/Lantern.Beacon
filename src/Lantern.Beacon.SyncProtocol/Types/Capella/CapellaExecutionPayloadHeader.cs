using Cortex.Containers;
using Nethermind.Int256;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Capella;

public class CapellaExecutionPayloadHeader(Hash32 parentHash,
    Bytes20 feeRecipientFeeRecipientAddress,
    Bytes32 stateRoot,
    Bytes32 receiptRoot,
    byte[] logsBloom,
    Bytes32 prevRandao,
    ulong blockNumber,
    ulong gasLimit,
    ulong gasUsed,
    ulong timestamp,
    List<byte> extraData,
    UInt256 baseFeePerGas,
    Hash32 blockHash,
    Bytes32 transactionsRoot,
    Bytes32 withdrawalsRoot) : Bellatrix.BellatrixExecutionPayloadHeader(parentHash, feeRecipientFeeRecipientAddress, stateRoot, receiptRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot)
{
    public Bytes32 WithdrawalsRoot { get; init; } = withdrawalsRoot;
    
    public bool Equals(CapellaExecutionPayloadHeader? other)
    {
        return other != null && ParentHash.Equals(other.ParentHash) && FeeRecipientAddress.Equals(other.FeeRecipientAddress) && StateRoot.Equals(other.StateRoot) && ReceiptsRoot.Equals(other.ReceiptsRoot) && LogsBloom.SequenceEqual(other.LogsBloom) && PrevRandoa.Equals(other.PrevRandoa) && BlockNumber.Equals(other.BlockNumber) && GasLimit.Equals(other.GasLimit) && GasUsed.Equals(other.GasUsed) && Timestamp.Equals(other.Timestamp) && ExtraData.SequenceEqual(other.ExtraData) && BaseFeePerGas.Equals(other.BaseFeePerGas) && BlockHash.Equals(other.BlockHash) && TransactionsRoot.Equals(other.TransactionsRoot) && WithdrawalsRoot.Equals(other.WithdrawalsRoot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is CapellaExecutionPayloadHeader other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(ParentHash);
        hash.Add(FeeRecipientAddress);
        hash.Add(StateRoot);
        hash.Add(ReceiptsRoot);
        hash.Add(LogsBloom);
        hash.Add(PrevRandoa);
        hash.Add(BlockNumber);
        hash.Add(GasLimit);
        hash.Add(GasUsed);
        hash.Add(Timestamp);
        
        foreach (var item in ExtraData)
        {
            hash.Add(item);
        }
        
        hash.Add(BaseFeePerGas);
        hash.Add(BlockHash);
        hash.Add(TransactionsRoot);
        hash.Add(WithdrawalsRoot);

        return hash.ToHashCode();
    }
    
    public new static CapellaExecutionPayloadHeader CreateDefault()
    {
        return new CapellaExecutionPayloadHeader(Hash32.Zero, new Bytes20(), new Bytes32(), new Bytes32(), new byte[Constants.BytesPerLogsBloom], new Bytes32(), 0, 0, 0, 0,
            [], UInt256.Zero, Hash32.Zero, new Bytes32(), new Bytes32());
    }
    
    public static int BytesLength => Bellatrix.BellatrixExecutionPayloadHeader.BytesLength + Bytes32.Length;
    
    public new static class Serializer
    {
        
    }
}