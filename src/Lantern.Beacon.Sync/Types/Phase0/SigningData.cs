using SszSharp;

namespace Lantern.Beacon.Sync.Types.Phase0;

public class SigningData : IEquatable<SigningData>
{
    [SszElement(0, "Vector[uint8, 32]")] 
    public byte[] ObjectRoot { get; init; }
    
    [SszElement(1, "Vector[uint8, 32]")] 
    public byte[] Domain { get; init; }
    
    public bool Equals(SigningData? other)
    {
        return other != null && ObjectRoot.SequenceEqual(other.ObjectRoot) && Domain.SequenceEqual(other.Domain);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is SigningData other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(ObjectRoot, Domain);
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<SigningData>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static SigningData CreateFrom(byte[] objectRoot, byte[] domain)
    {
        return new SigningData
        {
            ObjectRoot = objectRoot,
            Domain = domain
        };
    }
    
    public static SigningData CreateDefault()
    {
        return new SigningData
        {
            ObjectRoot = new byte[Constants.RootLength],
            Domain = new byte[Constants.DomainLength]
        };
    }
    
    public static byte[] Serialize(SigningData signingData, SizePreset preset)
    {
        return SszContainer.Serialize(signingData, preset);
    }
    
    public static SigningData Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<SigningData>(data, preset);
        return result.Item1;
    } 
}