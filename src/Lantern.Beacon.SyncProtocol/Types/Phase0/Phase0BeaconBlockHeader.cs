using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Phase0;

public class Phase0BeaconBlockHeader : IEquatable<Phase0BeaconBlockHeader>
{
    [SszElement(0, "uint64")]
    public ulong Slot { get; private init; }
    
    [SszElement(1, "uint64")]
    public ulong ProposerIndex { get; private init; } 
    
    [SszElement(2, "Vector[uint8, 32]")] 
    public byte[] ParentRoot { get; private init; } 
    
    [SszElement(3, "Vector[uint8, 32]")] 
    public byte[] StateRoot { get; private init; } 
    
    [SszElement(4, "Vector[uint8, 32]")]
    public byte[] BodyRoot { get; private init; } 
    
    public bool Equals(Phase0BeaconBlockHeader? other)
    {
        return other != null && Slot.Equals(other.Slot) && ProposerIndex.Equals(other.ProposerIndex) && ParentRoot.SequenceEqual(other.ParentRoot) && StateRoot.SequenceEqual(other.StateRoot) && BodyRoot.SequenceEqual(other.BodyRoot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Phase0BeaconBlockHeader other)
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
    
    public static Phase0BeaconBlockHeader CreateFrom(ulong slot, ulong proposerIndex, byte[] parentRoot, byte[] stateRoot, byte[] bodyRoot)
    {
        return new Phase0BeaconBlockHeader
        {
            Slot = slot,
            ProposerIndex = proposerIndex,
            ParentRoot = parentRoot,
            StateRoot = stateRoot,
            BodyRoot = bodyRoot
        };
    }
    
    public static Phase0BeaconBlockHeader CreateDefault()
    {
        return CreateFrom(0, 0, new byte[32], new byte[32], new byte[32]);
    }
    
    public static byte[] Serialize(Phase0BeaconBlockHeader beaconBlockHeader)
    {
        return SszContainer.Serialize(beaconBlockHeader);
    }
    
    public static Phase0BeaconBlockHeader Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<Phase0BeaconBlockHeader>(data, SizePreset.DefaultPreset);
        return result.Item1;
    } 
}