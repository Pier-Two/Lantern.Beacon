using SszSharp;

namespace Lantern.Beacon.Sync.Types.Altair;

public class LightClientBootstrapRequest : IEquatable<LightClientBootstrapRequest>
{
    [SszElement(0, "Vector[uint8, 32]")]
    public byte[] BlockRoot { get; init; }
    
    public bool Equals(LightClientBootstrapRequest? other)
    {
        return other != null && BlockRoot.SequenceEqual(other.BlockRoot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientBootstrapRequest other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(BlockRoot);
    }
    
    public static LightClientBootstrapRequest CreateFrom(byte[] blockRoot)
    {
        return new LightClientBootstrapRequest
        {
            BlockRoot = blockRoot
        };
    }
    
    public static byte[] Serialize(LightClientBootstrapRequest lightClientBootstrapRequest)
    {
        return SszContainer.Serialize(lightClientBootstrapRequest);
    }
    
    public static LightClientBootstrapRequest Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<LightClientBootstrapRequest>(data);
        return result.Item1;
    }
}