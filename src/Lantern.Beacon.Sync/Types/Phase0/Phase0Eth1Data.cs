using SszSharp;

namespace Lantern.Beacon.Sync.Types.Phase0;

public class Phase0Eth1Data : IEquatable<Phase0Eth1Data>
{
    [SszElement(0, "Vector[uint8, 32]")]
    public ulong DepositRoot { get; private init; }
    
    [SszElement(1, "uint64")]
    public ulong DepositCount { get; private init; } 
    
    [SszElement(2, "Vector[uint8, 32]")]
    public byte[] BlockHash { get; private init; }
    
    public bool Equals(Phase0Eth1Data? other)
    {
        return other != null && DepositRoot.Equals(other.DepositRoot) && DepositCount.Equals(other.DepositCount) && BlockHash.SequenceEqual(other.BlockHash);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Phase0Eth1Data other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(DepositRoot, DepositCount, BlockHash);
    }
    
    public static Phase0Eth1Data CreateFrom(ulong depositRoot, ulong depositCount, byte[] blockHash)
    {
        return new Phase0Eth1Data
        {
            DepositRoot = depositRoot,
            DepositCount = depositCount,
            BlockHash = blockHash
        };
    }
    
    public static Phase0Eth1Data CreateDefault()
    {
        return new Phase0Eth1Data
        {
            DepositRoot = 0,
            DepositCount = 0,
            BlockHash = new byte[32]
        };
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<Phase0Eth1Data>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static byte[] Serialize(Phase0Eth1Data phase0Eth1Data)
    {
        return SszContainer.Serialize(phase0Eth1Data);
    }
    
    public static Phase0Eth1Data Deserialize(byte[] bytes, SizePreset preset)
    {
        return SszContainer.Deserialize<Phase0Eth1Data>(bytes, preset).Item1;
    }
}