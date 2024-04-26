using Cortex.Containers;

namespace Lantern.Beacon.SyncProtocol.Types.Phase0;

public class Phase0BeaconBlockHeader(Slot slot,
    ValidatorIndex proposerIndex,
    Bytes32 parentRoot,
    Bytes32 stateRoot,
    Bytes32 bodyRoot) : IEquatable<Phase0BeaconBlockHeader>
{
    
    public Slot Slot { get; } = slot;
    
    public ValidatorIndex ProposerIndex { get; } = proposerIndex;
    
    public Bytes32 ParentRoot { get; } = parentRoot;
    
    public Bytes32 StateRoot { get; } = stateRoot;
    
    public Bytes32 BodyRoot { get; } = bodyRoot;
    
    public bool Equals(Phase0BeaconBlockHeader? other)
    {
        return other != null && Slot.Equals(other.Slot) && ParentRoot.Equals(other.ParentRoot) && StateRoot.Equals(other.StateRoot) && BodyRoot.Equals(other.BodyRoot);
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
    
    public static Phase0BeaconBlockHeader CreateDefault()
    {
        return new Phase0BeaconBlockHeader(Slot.Zero, new ValidatorIndex(0), new Bytes32(), new Bytes32(), new Bytes32());
    }
}