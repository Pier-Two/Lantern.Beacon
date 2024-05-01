using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types.Altair;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Capella;

public class CapellaLightClientHeader : AltairLightClientHeader
{
    [SszElement(1, "Container")]
    public CapellaExecutionPayloadHeader BellatrixExecution { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 4]")]
    public byte[][] ExecutionBranch { get; protected init; } 
    
    public bool Equals(CapellaLightClientHeader? other)
    {
        return other != null && Beacon.Equals(other.Beacon) && BellatrixExecution.Equals(other.BellatrixExecution) && ExecutionBranch.SequenceEqual(other.ExecutionBranch);
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
        return HashCode.Combine(Beacon, BellatrixExecution, ExecutionBranch);
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
            BellatrixExecution = bellatrixExecution,
            ExecutionBranch = executionBranch
        };
    }

    public new static CapellaLightClientHeader CreateDefault()
    {
        return CreateFrom(Phase0BeaconBlockHeader.CreateDefault(), CapellaExecutionPayloadHeader.CreateDefault(), new byte[Constants.ExecutionBranchDepth][]);
    }
    
    public new static int BytesLength => Phase0BeaconBlockHeader.BytesLength + CapellaExecutionPayloadHeader.BytesLength + Constants.ExecutionBranchDepth * Bytes32.Length + Constants.BytesPerLengthOffset;
    
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