using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Phase0;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Deneb;

public class DenebLightClientHeader : AltairLightClientHeader
{ 
    [SszElement(1, "Container")]
    public DenebExecutionPayloadHeader Execution { get; protected init; }
    
    [SszElement(2, "Vector[Vector[uint8, 32], 4]")]
    public byte[][] ExecutionBranch { get; protected init; } 
    
    public bool Equals(DenebLightClientHeader? other)
    {
        return other != null && Beacon.Equals(other.Beacon) && Execution.Equals(other.Execution) && ExecutionBranch.SequenceEqual(other.ExecutionBranch);
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