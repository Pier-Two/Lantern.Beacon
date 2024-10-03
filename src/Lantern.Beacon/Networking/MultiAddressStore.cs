using LiteDB;

namespace Lantern.Beacon.Networking;

public class MultiAddressStore : IEquatable<MultiAddressStore>
{
    [BsonId] 
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public string MultiAddress { get; init; }

    public bool Equals(MultiAddressStore? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return MultiAddress == other.MultiAddress;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MultiAddressStore)obj);
    }

    public override int GetHashCode()
    {
        return MultiAddress.GetHashCode();
    }
}