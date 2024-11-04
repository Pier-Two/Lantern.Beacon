using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientHeader : IEquatable<DenebLightClientHeader>
{
    [JsonPropertyName("beacon")] 
    public Phase0BeaconBlockHeader BeaconJson => Beacon;
    
    [JsonPropertyName("execution")]
    public DenebExecutionPayloadHeader ExecutionJson => Execution;
    
    [JsonPropertyName("execution_branch")]
    public string[] ExecutionBranchJson => Array.ConvertAll(ExecutionBranch, b => $"0x{Convert.ToHexString(b).ToLower()}");
    
    [JsonIgnore] 
    [SszElement(0, "Container")]
    public Phase0BeaconBlockHeader Beacon { get; protected init; } 
    
    [JsonIgnore] 
    [SszElement(1, "Container")]
    public DenebExecutionPayloadHeader Execution { get; protected init; }
    
    [JsonIgnore] 
    [SszElement(2, "Vector[Vector[uint8, 32], 4]")]
    public byte[][] ExecutionBranch { get; protected init; } 
    
    public bool Equals(DenebLightClientHeader? other)
    {
        if (other == null) return false;

        if (!Beacon.Equals(other.Beacon) || !Execution.Equals(other.Execution))
        {
            return false;
        }

        if (ExecutionBranch.Length != other.ExecutionBranch.Length)
        {
            return false;
        }

        for (int i = 0; i < ExecutionBranch.Length; i++)
        {
            if (!ExecutionBranch[i].SequenceEqual(other.ExecutionBranch[i]))
            {
                return false;
            }
        }

        return true;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is DenebLightClientHeader other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientHeader>(preset);
        return container.HashTreeRoot(this);
    }
    
    public override int GetHashCode()
    {
        var hash = new HashCode();
        
        hash.Add(base.GetHashCode());  
        
        if (Execution != null)
        {
            hash.Add(Execution.GetHashCode());
        }
        
        if (ExecutionBranch != null)
        {
            foreach (var branch in ExecutionBranch)
            {
                if (branch != null)
                {
                    foreach (var b in branch)
                    {
                        hash.Add(b);
                    }
                }
            }
        }

        return hash.ToHashCode();
    }
    
    public static DenebLightClientHeader CreateFromCapella(CapellaLightClientHeader pre)
    {
        return new DenebLightClientHeader
        {
            Beacon = pre.Beacon,
            Execution = DenebExecutionPayloadHeader.CreateFromCapella(pre.Execution),
            ExecutionBranch = pre.ExecutionBranch
        };
    }
    
    public static DenebLightClientHeader CreateFrom(Phase0BeaconBlockHeader beaconBlockHeader, DenebExecutionPayloadHeader execution, byte[][] executionBranch)
    {
        if (executionBranch.Length != Constants.ExecutionBranchDepth)
        {
            throw new ArgumentException($"Execution branch length must be {Constants.ExecutionBranchDepth}");
        }
        
        return new DenebLightClientHeader
        {
            Beacon = beaconBlockHeader,
            Execution = execution,
            ExecutionBranch = executionBranch
        };
    }

    public new static DenebLightClientHeader CreateDefault()
    {
        var executionBranch = new byte[Constants.ExecutionBranchDepth][];
        
        for (var i = 0; i < executionBranch.Length; i++)
        {
            executionBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(Phase0BeaconBlockHeader.CreateDefault(), DenebExecutionPayloadHeader.CreateDefault(), executionBranch);
    }
    
    public static byte[] Serialize(DenebLightClientHeader denebLightClientHeader, SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientHeader>(preset);
        var bytes = new byte[container.Length(denebLightClientHeader)];
        
        container.Serialize(denebLightClientHeader, bytes.AsSpan());
        
        return bytes;
    }
    
    public static DenebLightClientHeader Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<DenebLightClientHeader>(data, preset);
        return result.Item1;
    } 
}