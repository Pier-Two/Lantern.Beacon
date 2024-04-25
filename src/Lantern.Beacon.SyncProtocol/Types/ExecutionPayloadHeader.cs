using Cortex.Containers;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Int256;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class ExecutionPayloadHeader : IEquatable<ExecutionPayloadHeader>
{
    public ExecutionPayloadHeader(
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

    public bool Equals(ExecutionPayloadHeader? other)
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
        return ReferenceEquals(this, obj) || obj is ExecutionPayloadHeader other && Equals(other);
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

    public static ExecutionPayloadHeader CreateDefault()
    {
        return new ExecutionPayloadHeader(Hash32.Zero, new Bytes20(), new Bytes32(), new Bytes32(), new byte[Constants.BytesPerLogsBloom],
            new Bytes32(), 0, 0, 0, 0,
            [], UInt256.Zero, Hash32.Zero, new Bytes32());
    }
    
    public static int BytesLength => Hash32.Length + Bytes20.Length + Bytes32.Length + Bytes32.Length + Constants.BytesPerLogsBloom + Bytes32.Length + 
                                     sizeof(ulong) * 4 + Constants.BytesPerLengthOffset + Bytes32.Length + Hash32.Length + Root.Length;
    
    public static class Serializer
    {
        public static byte[] Serialize(ExecutionPayloadHeader header)
        {
            var result = new byte[BytesLength + header.ExtraData.Count];
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
        
        public static ExecutionPayloadHeader Deserialize(byte[] bytes)
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
            
            return new ExecutionPayloadHeader(parentHash, address, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot);
        }

        public static ExecutionPayloadHeader Deserialize(Span<byte> bytes)
        {
            Console.WriteLine("Total length: " + bytes.Length);
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
            
            return new ExecutionPayloadHeader(parentHash, address, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot);
        }
    }
}