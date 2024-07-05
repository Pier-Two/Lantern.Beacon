using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class ForkData : IEquatable<ForkData>
{
    [SszElement(0, "Vector[uint8, 4]")] 
    public byte[] CurrentVersion { get; init; }
    
    [SszElement(1, "Vector[uint8, 32]")] 
    public byte[] GenesisValidatorsRoot { get; init; }
    
    public bool Equals(ForkData? other)
    {
        return other != null && CurrentVersion.SequenceEqual(other.CurrentVersion) && GenesisValidatorsRoot.SequenceEqual(other.GenesisValidatorsRoot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ForkData other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(CurrentVersion, GenesisValidatorsRoot);
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<ForkData>(preset);
        return container.HashTreeRoot(this);
    }

    public static ForkData CreateFrom(byte[] currentVersion, byte[] genesisValidatorsRoot)
    {
        return new ForkData
        {
            CurrentVersion = currentVersion,
            GenesisValidatorsRoot = genesisValidatorsRoot
        };
    }
    
    public static ForkData CreateDefault()
    {
        return new ForkData
        {
            CurrentVersion = new byte[Constants.VersionLength],
            GenesisValidatorsRoot = new byte[Constants.RootLength]
        };
    }
    
    public static byte[] Serialize(ForkData forkData)
    {
        return SszContainer.Serialize(forkData);
    }
    
    public static ForkData Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<ForkData>(data, SizePreset.DefaultPreset);
        return result.Item1;
    } 
}