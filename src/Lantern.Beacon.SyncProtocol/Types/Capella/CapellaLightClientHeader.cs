using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types.Altair;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Capella;

public class CapellaLightClientHeader : AltairLightClientHeader
{
    [SszElement(1, "Container")]
    public CapellaExecutionPayloadHeader Execution { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 4]")]
    public byte[][] ExecutionBranch { get; protected init; } 
    
    public bool Equals(CapellaLightClientHeader? other)
    {
        return other != null && Beacon.Equals(other.Beacon) && Execution.Equals(other.Execution) && ExecutionBranch.SequenceEqual(other.ExecutionBranch);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is CapellaLightClientHeader other)
        {
            return Equals(other);
        }
        
        return false;
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
    
    public static CapellaLightClientHeader CreateFrom(Phase0BeaconBlockHeader beaconBlockHeader, CapellaExecutionPayloadHeader bellatrixExecution, byte[][] executionBranch)
    {
        if (executionBranch.Length != Constants.ExecutionBranchDepth)
        {
            throw new ArgumentException($"Execution branch length must be {Constants.ExecutionBranchDepth}");
        }
        
        return new CapellaLightClientHeader
        {
            Beacon = beaconBlockHeader,
            Execution = bellatrixExecution,
            ExecutionBranch = executionBranch
        };
    }

    public new static CapellaLightClientHeader CreateDefault()
    {
        var executionBranch = new byte[Constants.ExecutionBranchDepth][];
        
        for (var i = 0; i < executionBranch.Length; i++)
        {
            executionBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(Phase0BeaconBlockHeader.CreateDefault(), CapellaExecutionPayloadHeader.CreateDefault(), executionBranch);
    }
    
    public static byte[] Serialize(CapellaLightClientHeader capellaLightClientHeader)
    {
        var container = SszContainer.GetContainer<CapellaLightClientHeader>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(capellaLightClientHeader)];
        
        container.Serialize(capellaLightClientHeader, bytes.AsSpan());
        
        return bytes;
    }
    
    public static CapellaLightClientHeader Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<CapellaLightClientHeader>(data, SizePreset.MainnetPreset);
        return result.Item1;
    } 
}