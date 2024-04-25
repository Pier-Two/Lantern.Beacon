using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class LightClientFinalityUpdate(LightClientHeader attestedHeader,
    LightClientHeader finalizedHeader,
    Bytes32[] finalityBranch, 
    SyncAggregate syncAggregate,
    Slot signatureSlot) : IEquatable<LightClientFinalityUpdate>
{
    public LightClientHeader AttestedHeader { get; init; } = attestedHeader;
    
    public LightClientHeader FinalizedHeader { get; init; } = finalizedHeader;
    
    public Bytes32[] FinalityBranch { get; init; } = finalityBranch;
    
    public SyncAggregate SyncAggregate { get; init; } = syncAggregate;
    
    public Slot SignatureSlot { get; init; } = signatureSlot;
    
    public bool Equals(LightClientFinalityUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientFinalityUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, FinalizedHeader, FinalityBranch, SyncAggregate, SignatureSlot);
    }
    
    public static LightClientFinalityUpdate CreateDefault()
    {
        return new LightClientFinalityUpdate(LightClientHeader.CreateDefault(), LightClientHeader.CreateDefault(), new Bytes32[Constants.FinalizedRootGIndex], SyncAggregate.CreateDefault(), Slot.Zero);
    }
    
    public static int BytesLength => LightClientHeader.BytesLength + LightClientHeader.BytesLength + Constants.FinalityBranchDepth * Bytes32.Length + SyncAggregate.BytesLength + sizeof(ulong);
    
    public static class Serializer
    {
        public static byte[] Serialize(LightClientFinalityUpdate finalityUpdate)
        {
            var result = new byte[BytesLength];
            var attestedHeaderBytes = LightClientHeader.Serializer.Serialize(finalityUpdate.AttestedHeader);
            var finalizedHeaderBytes = LightClientHeader.Serializer.Serialize(finalityUpdate.FinalizedHeader);
            var syncAggregateBytes = SyncAggregate.Serializer.Serialize(finalityUpdate.SyncAggregate);
            
            Array.Copy(attestedHeaderBytes, 0, result, 0, LightClientHeader.BytesLength);
            Array.Copy(finalizedHeaderBytes, 0, result, LightClientHeader.BytesLength, LightClientHeader.BytesLength);

            var offset = LightClientHeader.BytesLength * 2;
            
            foreach (var array in finalityUpdate.FinalityBranch)
            {
                Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
                offset += Bytes32.Length;
            }
            
            Array.Copy(syncAggregateBytes, 0, result, offset, SyncAggregate.BytesLength);
            
            Ssz.Encode(result.AsSpan(offset + SyncAggregate.BytesLength, sizeof(ulong)), (ulong)finalityUpdate.SignatureSlot);
            
            Console.WriteLine(Convert.ToHexString(result));
            
            return result;
        }
        
        public static LightClientFinalityUpdate Deserialize(byte[] bytes)
        {
            var attestedHeader = LightClientHeader.Serializer.Deserialize(bytes.AsSpan(0, LightClientHeader.BytesLength));
            var finalizedHeader = LightClientHeader.Serializer.Deserialize(bytes.AsSpan(LightClientHeader.BytesLength, LightClientHeader.BytesLength));
            var finalityBranch = new Bytes32[Constants.FinalityBranchDepth];
            var offset = 2 * LightClientHeader.BytesLength;
            
            for (var i = 0; i < Constants.FinalityBranchDepth; i++)
            {
                finalityBranch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
                offset += Bytes32.Length;
            }
            
            var syncAggregate = SyncAggregate.Serializer.Deserialize(bytes.AsSpan(offset, SyncAggregate.BytesLength));
            var signatureSlot = Ssz.DecodeULong(bytes.AsSpan(offset + SyncAggregate.BytesLength, sizeof(ulong)));
            
            return new LightClientFinalityUpdate(attestedHeader, finalizedHeader, finalityBranch, syncAggregate, new Slot(signatureSlot));
        }
    }
}