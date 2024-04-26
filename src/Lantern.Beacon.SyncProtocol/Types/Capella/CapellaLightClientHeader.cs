using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.SimpleSerialize;
using Lantern.Beacon.SyncProtocol.Types.Altair;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Capella;

public class CapellaLightClientHeader(Phase0BeaconBlockHeader beacon, 
    CapellaExecutionPayloadHeader bellatrixExecution,
    Bytes32[] executionBranch) : Altair.AltairLightClientHeader(beacon)
{
    public CapellaExecutionPayloadHeader BellatrixExecution { get; init; } = bellatrixExecution;
    
    public Bytes32[] ExecutionBranch { get; init; } = executionBranch;
    
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
    
    public new static CapellaLightClientHeader CreateDefault()
    {
        return new CapellaLightClientHeader(Phase0BeaconBlockHeader.CreateDefault(), CapellaExecutionPayloadHeader.CreateDefault(), new Bytes32[Constants.ExecutionBranchDepth]);
    }
    
    public new static int BytesLength => Phase0BeaconBlockHeader.BytesLength + CapellaExecutionPayloadHeader.BytesLength + Constants.ExecutionBranchDepth * Bytes32.Length + Constants.BytesPerLengthOffset;
}