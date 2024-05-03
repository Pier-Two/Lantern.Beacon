using System.Numerics;
using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types.Bellatrix;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Capella;

public class CapellaExecutionPayloadHeader : BellatrixExecutionPayloadHeader
{
    [SszElement(14, "Vector[uint8, 32]")]
    public byte[] WithdrawalsRoot { get; protected init; }
    
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
    
    public static CapellaExecutionPayloadHeader CreateFrom(
        byte[] parentHash, 
        byte[] feeRecipientAddress, 
        byte[] stateRoot, 
        byte[] receiptsRoot, 
        byte[] logsBloom, 
        byte[] prevRandoa, 
        ulong blockNumber, 
        ulong gasLimit, 
        ulong gasUsed, 
        ulong timestamp, 
        byte[] extraData, 
        BigInteger baseFeePerGas, 
        byte[] blockHash, 
        byte[] transactionsRoot, 
        byte[] withdrawalsRoot)
    {
        return new CapellaExecutionPayloadHeader
        {
            ParentHash = parentHash,
            FeeRecipientAddress = feeRecipientAddress,
            StateRoot = stateRoot,
            ReceiptsRoot = receiptsRoot,
            LogsBloom = logsBloom,
            PrevRandoa = prevRandoa,
            BlockNumber = blockNumber,
            GasLimit = gasLimit,
            GasUsed = gasUsed,
            Timestamp = timestamp,
            ExtraData = extraData,
            BaseFeePerGas = baseFeePerGas,
            BlockHash = blockHash,
            TransactionsRoot = transactionsRoot,
            WithdrawalsRoot = withdrawalsRoot
        };
    }
    
    public static CapellaExecutionPayloadHeader CreateDefault()
    {
        return CreateFrom(new byte[32], new byte[20], new byte[32], new byte[32], new byte[Constants.BytesPerLogsBloom],
            new byte[32], 0, 0, 0, 0,
            [], BigInteger.Zero, new byte[32], new byte[32], new byte[32]);
    }
    
    public static byte[] Serialize(CapellaExecutionPayloadHeader capellaExecutionPayloadHeader)
    {
        var container = SszContainer.GetContainer<CapellaExecutionPayloadHeader>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(capellaExecutionPayloadHeader)];
        
        container.Serialize(capellaExecutionPayloadHeader, bytes.AsSpan());
        
        return bytes;
    }
    
    public new static CapellaExecutionPayloadHeader Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<CapellaExecutionPayloadHeader>(data, SizePreset.MainnetPreset);
        return result.Item1;
    } 
}