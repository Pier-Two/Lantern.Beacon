using System.Numerics;
using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Bellatrix;

public class BellatrixExecutionPayloadHeader : IEquatable<BellatrixExecutionPayloadHeader>
{
    [SszElement(0, "Vector[uint8, 32]")]
    public byte[] ParentHash { get; protected init; }

    [SszElement(1, "Vector[uint8, 20]")]
    public byte[] FeeRecipientAddress { get; protected init; }

    [SszElement(2, "Vector[uint8, 32]")]
    public byte[] StateRoot { get; protected init; } 

    [SszElement(3, "Vector[uint8, 32]")]
    public byte[] ReceiptsRoot { get; protected init; } 

    [SszElement(4, "Vector[uint8, BYTES_PER_LOGS_BLOOM]")]
    public byte[] LogsBloom { get; protected init; } 
    
    [SszElement(5, "Vector[uint8, 32]")]
    public byte[] PrevRandoa { get; protected init; } 

    [SszElement(6, "uint64")]
    public ulong BlockNumber { get; protected init; }

    [SszElement(7, "uint64")]
    public ulong GasLimit { get; protected init; } 

    [SszElement(8, "uint64")]
    public ulong GasUsed { get; protected init; } 

    [SszElement(9, "uint64")]
    public ulong Timestamp { get; protected init; } 

    [SszElement(10, "List[uint8, MAX_EXTRA_DATA_BYTES]")]
    public byte[] ExtraData { get; protected init; } 

    [SszElement(11, "uint256")]
    public BigInteger BaseFeePerGas { get; protected init; } 
    
    [SszElement(12, "Vector[uint8, 32]")]
    public byte[] BlockHash { get; protected init; }

    [SszElement(13, "Vector[uint8, 32]")]
    public byte[] TransactionsRoot { get; protected init; } 

    public bool Equals(BellatrixExecutionPayloadHeader? other)
    {
        return other != null && ParentHash.SequenceEqual(other.ParentHash) && FeeRecipientAddress.SequenceEqual(other.FeeRecipientAddress) &&
               StateRoot.SequenceEqual(other.StateRoot) && ReceiptsRoot.SequenceEqual(other.ReceiptsRoot) &&
               LogsBloom.SequenceEqual(other.LogsBloom) && PrevRandoa.SequenceEqual(other.PrevRandoa) &&
               BlockNumber == other.BlockNumber && GasLimit == other.GasLimit && GasUsed == other.GasUsed &&
               Timestamp == other.Timestamp && ExtraData.SequenceEqual(other.ExtraData) &&
               BaseFeePerGas.Equals(other.BaseFeePerGas) && BlockHash.SequenceEqual(other.BlockHash) &&
               TransactionsRoot.SequenceEqual(other.TransactionsRoot);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is BellatrixExecutionPayloadHeader other && Equals(other);
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

        return hash.ToHashCode();
    }

    public static BellatrixExecutionPayloadHeader CreateFrom(        
        byte[] parentHash,
        byte[] feeRecipientFeeRecipientAddress,
        byte[] stateRoot,
        byte[] receiptRoot,
        byte[] logsBloom,
        byte[] prevRandao,
        ulong blockNumber,
        ulong gasLimit,
        ulong gasUsed,
        ulong timestamp,
        byte[] extraData,
        BigInteger baseFeePerGas,
        byte[] blockHash,
        byte[] transactionsRoot)
    {
        if (logsBloom.Length > Constants.BytesPerLogsBloom)
        {
            throw new ArgumentException($"Length of logs bloom must be less than {Constants.BytesPerLogsBloom} bytes long");
        }
        
        if(extraData.Length > Constants.MaxExtraDataBytes)
        {
            throw new ArgumentException($"Length of extra data must be less than {Constants.MaxExtraDataBytes} bytes long");
        }
        
        return new BellatrixExecutionPayloadHeader
        {
            ParentHash = parentHash,
            FeeRecipientAddress = feeRecipientFeeRecipientAddress,
            StateRoot = stateRoot,
            ReceiptsRoot = receiptRoot,
            LogsBloom = logsBloom,
            PrevRandoa = prevRandao,
            BlockNumber = blockNumber,
            GasLimit = gasLimit,
            GasUsed = gasUsed,
            Timestamp = timestamp,
            ExtraData = extraData,
            BaseFeePerGas = baseFeePerGas,
            BlockHash = blockHash,
            TransactionsRoot = transactionsRoot
        };
    }
    
    public static BellatrixExecutionPayloadHeader CreateDefault()
    {
        return CreateFrom(new byte[32], new byte[20], new byte[32], new byte[32], new byte[Constants.BytesPerLogsBloom],
            new byte[32], 0, 0, 0, 0,
            [], BigInteger.Zero, new byte[32], new byte[32]);
    }
    
    public static byte[] Serialize(BellatrixExecutionPayloadHeader bellatrixExecutionPayloadHeader, SizePreset preset)
    {
        var container = SszContainer.GetContainer<BellatrixExecutionPayloadHeader>(preset);
        var bytes = new byte[container.Length(bellatrixExecutionPayloadHeader)];
        
        container.Serialize(bellatrixExecutionPayloadHeader, bytes.AsSpan());
        
        return bytes;
    }
    
    public static BellatrixExecutionPayloadHeader Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<BellatrixExecutionPayloadHeader>(data, preset);
        return result.Item1;
    } 
}