using Cortex.Containers;

namespace Lantern.Beacon.Sync.Types.Basic;

public class Root : Bytes32
{
    public static Root Zero { get; } = new();
    
    public Root() : base()
    {
    }
    
    public Root(ReadOnlySpan<byte> span) : base(span)
    {
    }
    
    public override bool Equals(object obj)
    {
        return obj is Root root && base.Equals(root);
    }

    public bool Equals(Root other)
    {
        return base.Equals(other);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}