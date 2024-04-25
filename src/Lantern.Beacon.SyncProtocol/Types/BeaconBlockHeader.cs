using System.Runtime.Serialization;
using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class BeaconBlockHeader(Slot slot,
    ValidatorIndex proposerIndex,
    Bytes32 parentRoot,
    Bytes32 stateRoot,
    Bytes32 bodyRoot) : IEquatable<BeaconBlockHeader>
{
    
    public Slot Slot { get; } = slot;
    
    public ValidatorIndex ProposerIndex { get; } = proposerIndex;
    
    public Bytes32 ParentRoot { get; } = parentRoot;
    
    public Bytes32 StateRoot { get; } = stateRoot;
    
    public Bytes32 BodyRoot { get; } = bodyRoot;
    
    public bool Equals(BeaconBlockHeader? other)
    {
        return other != null && Slot.Equals(other.Slot) && ParentRoot.Equals(other.ParentRoot) && StateRoot.Equals(other.StateRoot) && BodyRoot.Equals(other.BodyRoot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is BeaconBlockHeader other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Slot, ParentRoot, StateRoot, BodyRoot);
    }
    
    public static int BytesLength => 2 * sizeof(ulong) + 3 * Bytes32.Length;
    
    public static BeaconBlockHeader CreateDefault()
    {
        return new BeaconBlockHeader(Slot.Zero, new ValidatorIndex(0), new Bytes32(), new Bytes32(), new Bytes32());
    }
    
    public static class Serializer
    {
        public static byte[] Serialize(BeaconBlockHeader header)
        {
            var result = new byte[BytesLength];

            Ssz.Encode(result.AsSpan(0, sizeof(ulong)), (ulong)header.Slot);
            Ssz.Encode(result.AsSpan(sizeof(ulong), sizeof(ulong)), (ulong)header.ProposerIndex);
            Ssz.Encode(result.AsSpan(2 * sizeof(ulong), Bytes32.Length), (ReadOnlySpan<byte>)header.ParentRoot);
            Ssz.Encode(result.AsSpan(2 * sizeof(ulong) + Bytes32.Length, Bytes32.Length), (ReadOnlySpan<byte>)header.StateRoot);
            Ssz.Encode(result.AsSpan(2 * sizeof(ulong) + 2 * Bytes32.Length, Bytes32.Length), (ReadOnlySpan<byte>)header.BodyRoot);

            return result;
        }
        
        public static BeaconBlockHeader Deserialize(byte[] bytes)
        {
            var slot = new Slot(Ssz.DecodeULong(bytes.AsSpan(0, sizeof(ulong))));
            var proposerIndex= new ValidatorIndex(Ssz.DecodeULong(bytes.AsSpan(sizeof(ulong), sizeof(ulong))));
            var parentRoot = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(2 * sizeof(ulong), Bytes32.Length)).AsSpan());
            var stateRoot =
                new Bytes32(
                    Ssz.DecodeBytes32(bytes.AsSpan(2 * sizeof(ulong) + Bytes32.Length, Bytes32.Length)).AsSpan());
            var bodyRoot = new Bytes32(
                Ssz.DecodeBytes32(bytes.AsSpan(2 * sizeof(ulong) + 2 * Bytes32.Length, Bytes32.Length)).AsSpan());
            
            return new BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        }

        public static BeaconBlockHeader Deserialize(Span<byte> bytes)
        {
            var slot = new Slot(Ssz.DecodeULong(bytes[..sizeof(ulong)]));
            var proposerIndex = new ValidatorIndex(Ssz.DecodeULong(bytes.Slice(sizeof(ulong), sizeof(ulong))));
            var parentRoot = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(2 * sizeof(ulong), Bytes32.Length)).AsSpan());
            var stateRoot = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(2 * sizeof(ulong) + Bytes32.Length, Bytes32.Length)).AsSpan());
            var bodyRoot = new Bytes32(Ssz.DecodeBytes32(bytes.Slice(2 * sizeof(ulong) + 2 * Bytes32.Length, Bytes32.Length)).AsSpan());
    
            return new BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        }
    } 
}