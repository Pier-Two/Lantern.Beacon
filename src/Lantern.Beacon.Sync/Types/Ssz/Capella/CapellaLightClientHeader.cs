using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Capella;

public class CapellaLightClientHeader : IEquatable<CapellaLightClientHeader>
{
    [SszElement(0, "Container")]
    public Phase0BeaconBlockHeader Beacon { get; protected init; }
    
    [SszElement(1, "Container")]
    public CapellaExecutionPayloadHeader Execution { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 4]")]
    public byte[][] ExecutionBranch { get; protected init; } 
    
    public bool Equals(CapellaLightClientHeader? other)
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

        for (var i = 0; i < ExecutionBranch.Length; i++)
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

    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<CapellaLightClientHeader>(preset);
        return container.HashTreeRoot(this);
    }

    public static CapellaLightClientHeader CreateFromAltair(AltairLightClientHeader pre)
    {
        var executionBranch = new byte[Constants.ExecutionBranchDepth][];

        for (var i = 0; i < executionBranch.Length; i++)
        {
            executionBranch[i] = new byte[Constants.RootLength];
        }
        
        return new CapellaLightClientHeader
        {
            Beacon = pre.Beacon,
            Execution = CapellaExecutionPayloadHeader.CreateDefault(),
            ExecutionBranch = executionBranch
        };
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
    
    public static byte[] Serialize(CapellaLightClientHeader capellaLightClientHeader, SizePreset preset)
    {
        var container = SszContainer.GetContainer<CapellaLightClientHeader>(preset);
        var bytes = new byte[container.Length(capellaLightClientHeader)];
        
        container.Serialize(capellaLightClientHeader, bytes.AsSpan());
        
        return bytes;
    }
    
    public static CapellaLightClientHeader Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<CapellaLightClientHeader>(data, preset);
        return result.Item1;
    } 
}