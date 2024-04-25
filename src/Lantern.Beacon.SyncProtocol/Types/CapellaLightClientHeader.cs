using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class CapellaLightClientHeader(BeaconBlockHeader beacon, 
    CapellaExecutionPayloadHeader execution,
    Bytes32[] executionBranch) : LightClientHeader(beacon)
{
    public CapellaExecutionPayloadHeader Execution { get; init; } = execution;
    
    public Bytes32[] ExecutionBranch { get; init; } = executionBranch;
    
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
        return HashCode.Combine(Beacon, Execution, ExecutionBranch);
    }
    
    public new static CapellaLightClientHeader CreateDefault()
    {
        return new CapellaLightClientHeader(BeaconBlockHeader.CreateDefault(), CapellaExecutionPayloadHeader.CreateDefault(), new Bytes32[Constants.ExecutionBranchDepth]);
    }
    
    public new static int BytesLength => BeaconBlockHeader.BytesLength + CapellaExecutionPayloadHeader.BytesLength + Constants.ExecutionBranchDepth * Bytes32.Length + Constants.BytesPerLengthOffset;
    
    public new static class Serializer
    {
        public static byte[] Serialize(CapellaLightClientHeader header)
        {
            var result = new byte[BytesLength + header.Execution.ExtraData.Count];
            var beaconBytes = BeaconBlockHeader.Serializer.Serialize(header.Beacon);
            var offset = 0;
            
            Array.Copy(beaconBytes, 0, result, 0, BeaconBlockHeader.BytesLength);
             offset += BeaconBlockHeader.BytesLength;
            
            Ssz.Encode(result.AsSpan(offset, Constants.BytesPerLengthOffset), Constants.ExecutionBranchDepth * Bytes32.Length + offset + Constants.BytesPerLengthOffset);
            offset += Constants.BytesPerLengthOffset;
            
            foreach (var array in header.ExecutionBranch)
            {
                Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
                offset += Bytes32.Length;
            }

            var executionBytes = CapellaExecutionPayloadHeader.Serializer.Serialize(header.Execution);
            Array.Copy(executionBytes, 0, result, offset, executionBytes.Length);
            
            return result;
        }
        
        public static CapellaLightClientHeader Deserialize(byte[] bytes)
        {
            var offset = 0;
            var beacon = BeaconBlockHeader.Serializer.Deserialize(bytes.AsSpan(0, BeaconBlockHeader.BytesLength));
            offset += BeaconBlockHeader.BytesLength;
            
            var executionPayloadHeaderOffset = BitConverter.ToInt32(bytes.AsSpan(offset, Constants.BytesPerLengthOffset));
            offset += Constants.BytesPerLengthOffset;
            
            var executionBranch = new Bytes32[Constants.ExecutionBranchDepth];
            for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
            {
                executionBranch[i] = new Bytes32(bytes.AsSpan(offset, Bytes32.Length));
                offset += Bytes32.Length;
            }
            
            var execution = CapellaExecutionPayloadHeader.Serializer.Deserialize(bytes.AsSpan(executionPayloadHeaderOffset,  bytes.Length - executionPayloadHeaderOffset));
            
            return new CapellaLightClientHeader(beacon, execution, executionBranch);
        }
        
        public static CapellaLightClientHeader Deserialize(Span<byte> bytes)
        {
            var offset = 0;
            var beacon = BeaconBlockHeader.Serializer.Deserialize(bytes[..BeaconBlockHeader.BytesLength]);
            offset += BeaconBlockHeader.BytesLength;
            
            var executionPayloadHeaderOffset = BitConverter.ToInt32(bytes.Slice(offset, Constants.BytesPerLengthOffset));
            offset += Constants.BytesPerLengthOffset;
            
            var executionBranch = new Bytes32[Constants.ExecutionBranchDepth];
            for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
            {
                executionBranch[i] = new Bytes32(bytes.Slice(offset, Bytes32.Length));
                offset += Bytes32.Length;
            }
            
            var execution = CapellaExecutionPayloadHeader.Serializer.Deserialize(bytes.Slice(executionPayloadHeaderOffset,  bytes.Length - executionPayloadHeaderOffset));
            
            return new CapellaLightClientHeader(beacon, execution, executionBranch);
        }
    }
    
    
    
    
}