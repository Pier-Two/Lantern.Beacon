using Cortex.Containers;

namespace Lantern.Beacon.Sync.Types;

public class Bytes20 : IEquatable<Bytes20>
{
    public const int Length = 20;

    private readonly byte[] _bytes;

    public Bytes20()
    {
        _bytes = new byte[Length];
    }
    
    public Bytes20(ReadOnlySpan<byte> span)
    {
        if (span.Length != Length)
        {
            throw new ArgumentOutOfRangeException(nameof(span), span.Length, $"{nameof(Bytes32)} must have exactly {Length} bytes");
        }
        _bytes = span.ToArray();
    }

    public static explicit operator Bytes20(byte[] bytes) => new Bytes20(bytes);

    public static explicit operator Bytes20(Span<byte> span) => new Bytes20(span);

    public static explicit operator Bytes20(ReadOnlySpan<byte> span) => new Bytes20(span);

    public static explicit operator ReadOnlySpan<byte>(Bytes20 hash) => hash.AsSpan();

    public ReadOnlySpan<byte> AsSpan()
    {
        return new ReadOnlySpan<byte>(_bytes);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Bytes20 bytes20)
        {
            return Equals(bytes20);
        }

        return false;
    }

    public bool Equals(Bytes20? other)
    {
        return other != null &&
               _bytes.SequenceEqual(other._bytes);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in _bytes)
        {
            hash.Add(b);
        }
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return BitConverter.ToString(_bytes).Replace("-", "");
    }
    
    public static class Serializer
    {
        public static byte[] Serialize(Bytes20 bytes20)
        {
            return bytes20._bytes;
        }
        
        public static Bytes20 Deserialize(byte[] bytes)
        {
            return new Bytes20(bytes);
        }
    }
}