using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using Nethermind.Core.Crypto;
using Nethermind.Int256;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Bellatrix;

public class BellatrixExecutionPayloadHeader : IEquatable<BellatrixExecutionPayloadHeader>
{
    public BellatrixExecutionPayloadHeader(
        Hash32 parentHash,
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
        Bytes32 transactionsRoot)
    {
        if (logsBloom.Length > Constants.BytesPerLogsBloom)
        {
            throw new ArgumentException($"Length of logs bloom must be less than {Constants.BytesPerLogsBloom} bytes long");
        }
        
        LogsBloom = new byte[Constants.BytesPerLogsBloom];
        Array.Copy(logsBloom, LogsBloom, Math.Min(logsBloom.Length, Constants.BytesPerLogsBloom));
        
        ParentHash = parentHash;
        FeeRecipientAddress = feeRecipientFeeRecipientAddress;
        StateRoot = stateRoot;
        ReceiptsRoot = receiptRoot;
        PrevRandoa = prevRandao;
        BlockNumber = blockNumber;
        GasLimit = gasLimit;
        GasUsed = gasUsed;
        Timestamp = timestamp;
        ExtraData = extraData;
        BaseFeePerGas = baseFeePerGas;
        BlockHash = blockHash;
        TransactionsRoot = transactionsRoot;
    }
    
    public Hash32 ParentHash { get; }

    public Bytes20 FeeRecipientAddress { get; }

    public Bytes32 StateRoot { get; } 

    public Bytes32 ReceiptsRoot { get; } 

    public byte[] LogsBloom { get; } 

    public Bytes32 PrevRandoa { get; } 

    public ulong BlockNumber { get; }

    public ulong GasLimit { get; } 

    public ulong GasUsed { get; } 

    public ulong Timestamp { get; } 

    public List<byte> ExtraData { get; } 

    public UInt256 BaseFeePerGas { get; } 

    public Hash32 BlockHash { get; }

    public Bytes32 TransactionsRoot { get; } 

    public bool Equals(BellatrixExecutionPayloadHeader? other)
    {
        return other != null && ParentHash.Equals(other.ParentHash) && FeeRecipientAddress.Equals(other.FeeRecipientAddress) &&
               StateRoot.Equals(other.StateRoot) && ReceiptsRoot.Equals(other.ReceiptsRoot) &&
               LogsBloom.SequenceEqual(other.LogsBloom) && PrevRandoa.Equals(other.PrevRandoa) &&
               BlockNumber == other.BlockNumber && GasLimit == other.GasLimit && GasUsed == other.GasUsed &&
               Timestamp == other.Timestamp && ExtraData.SequenceEqual(other.ExtraData) &&
               BaseFeePerGas.Equals(other.BaseFeePerGas) && BlockHash.Equals(other.BlockHash) &&
               TransactionsRoot.Equals(other.TransactionsRoot);
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

    public static BellatrixExecutionPayloadHeader CreateDefault()
    {
        return new BellatrixExecutionPayloadHeader(Hash32.Zero, new Bytes20(), new Bytes32(), new Bytes32(), new byte[Constants.BytesPerLogsBloom],
            new Bytes32(), 0, 0, 0, 0,
            [], UInt256.Zero, Hash32.Zero, new Bytes32());
    }
    
    public static int BytesLength => Hash32.Length + Bytes20.Length + Bytes32.Length + Bytes32.Length + Constants.BytesPerLogsBloom + Bytes32.Length + 
                                     sizeof(ulong) * 4 + Constants.BytesPerLengthOffset + Bytes32.Length + Hash32.Length + Root.Length;
    
    public static class Serializer
    {
        
    }
}