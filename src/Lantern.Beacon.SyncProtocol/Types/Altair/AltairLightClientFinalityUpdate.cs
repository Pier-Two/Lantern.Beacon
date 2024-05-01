using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientFinalityUpdate : IEquatable<AltairLightClientFinalityUpdate>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader AttestedHeader { get; protected init; }
    
    [SszElement(1, "Container")]
    public AltairLightClientHeader FinalizedHeader { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 6]")]
    public byte[][] FinalityBranch { get; protected init; } 
    
    [SszElement(3, "Container")]
    public AltairSyncAggregate AltairSyncAggregate { get; protected init; } 
    
    [SszElement(4, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(AltairLightClientFinalityUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && AltairSyncAggregate.Equals(other.AltairSyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientFinalityUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, FinalizedHeader, FinalityBranch, AltairSyncAggregate, SignatureSlot);
    }
    
    public static AltairLightClientFinalityUpdate CreateFrom(
        AltairLightClientHeader altairLightClientHeader, 
        AltairLightClientHeader finalizedHeader, 
        byte[][] finalityBranch, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        if (finalityBranch.Length != Constants.FinalityBranchDepth)
        {
            throw new ArgumentException($"Finalized branch length must be {Constants.FinalityBranchDepth}");
        }
        
        return new AltairLightClientFinalityUpdate
        {
            AttestedHeader = altairLightClientHeader,
            FinalizedHeader = finalizedHeader,
            FinalityBranch = finalityBranch,
            AltairSyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }

    
    public static AltairLightClientFinalityUpdate CreateDefault()
    {
        return CreateFrom(AltairLightClientHeader.CreateDefault(), AltairLightClientHeader.CreateDefault(), new byte[Constants.FinalityBranchDepth][], AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairLightClientHeader.BytesLength + Constants.FinalityBranchDepth * Bytes32.Length + AltairSyncAggregate.BytesLength + sizeof(ulong);
    
    public static byte[] Serialize(AltairLightClientFinalityUpdate altairLightClientFinalityUpdateUpdate)
    {
        var container = SszContainer.GetContainer<AltairLightClientFinalityUpdate>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(altairLightClientFinalityUpdateUpdate)];
        
        container.Serialize(altairLightClientFinalityUpdateUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientFinalityUpdate Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairLightClientFinalityUpdate>(data, SizePreset.MainnetPreset);
        return result.Item1;
    }
}