using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class Phase0BeaconBlockHeader : IEquatable<Phase0BeaconBlockHeader>
{
    [JsonPropertyName("slot")]
    public string SlotString => Slot.ToString(); 

    [JsonPropertyName("proposer_index")]
    public string ProposerIndexString => ProposerIndex.ToString(); 

    [JsonPropertyName("parent_root")]
    public string ParentRootHex => $"0x{Convert.ToHexString(ParentRoot).ToLower()}"; 

    [JsonPropertyName("state_root")]
    public string StateRootHex => $"0x{Convert.ToHexString(StateRoot).ToLower()}"; 

    [JsonPropertyName("body_root")]
    public string BodyRootHex => $"0x{Convert.ToHexString(BodyRoot).ToLower()}"; 
    
    [JsonIgnore]
    [SszElement(0, "uint64")]
    public ulong Slot { get; private init; }
    
    [JsonIgnore]
    [SszElement(1, "uint64")]
    public ulong ProposerIndex { get; private init; } 
    
    [JsonIgnore]
    [SszElement(2, "Vector[uint8, 32]")] 
    public byte[] ParentRoot { get; private init; } 
    
    [JsonIgnore]
    [SszElement(3, "Vector[uint8, 32]")] 
    public byte[] StateRoot { get; private init; } 
    
    [JsonIgnore]
    [SszElement(4, "Vector[uint8, 32]")]
    public byte[] BodyRoot { get; private init; } 
    
    [JsonIgnore]
    public string HashTreeRootString => Convert.ToHexString(GetHashTreeRoot(SizePreset.MainnetPreset, this));
    
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
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<Phase0BeaconBlockHeader>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static byte[] GetHashTreeRoot(SizePreset preset, Phase0BeaconBlockHeader blockHeader)
    {
        var container = SszContainer.GetContainer<Phase0BeaconBlockHeader>(preset);
        return container.HashTreeRoot(blockHeader);
    }
    
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
    
    public static byte[] Serialize(Phase0BeaconBlockHeader beaconBlockHeader, SizePreset preset)
    {
        return SszContainer.Serialize(beaconBlockHeader, preset);
    }
    
    public static Phase0BeaconBlockHeader Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<Phase0BeaconBlockHeader>(data, preset);
        return result.Item1;
    } 
    
    public static byte[] ConvertToJsonBytes(Phase0BeaconBlockHeader header)
    {
        var jsonString = JsonSerializer.Serialize(header);
        return Encoding.UTF8.GetBytes(jsonString);
    }
}