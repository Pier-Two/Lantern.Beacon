using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Altair;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Capella;

public class CapellaLightClientFinalityUpdate : IEquatable<CapellaLightClientFinalityUpdate>
{
    [SszElement(0, "Container")]
    public CapellaLightClientHeader AttestedHeader { get; protected init; }
    
    [SszElement(1, "Container")]
    public CapellaLightClientHeader FinalizedHeader { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 6]")]
    public byte[][] FinalityBranch { get; protected init; } 
    
    [SszElement(3, "Container")]
    public AltairSyncAggregate SyncAggregate { get; protected init; } 
    
    [SszElement(4, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(CapellaLightClientFinalityUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is CapellaLightClientFinalityUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, FinalizedHeader, FinalityBranch, SyncAggregate, SignatureSlot);
    }
    
    public static CapellaLightClientFinalityUpdate CreateFrom(
        CapellaLightClientHeader altairLightClientHeader, 
        CapellaLightClientHeader finalizedHeader, 
        byte[][] finalityBranch, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        if (finalityBranch.Length != Constants.FinalityBranchDepth)
        {
            throw new ArgumentException($"Finalized branch length must be {Constants.FinalityBranchDepth}");
        }
        
        return new CapellaLightClientFinalityUpdate
        {
            AttestedHeader = altairLightClientHeader,
            FinalizedHeader = finalizedHeader,
            FinalityBranch = finalityBranch,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }

    public static CapellaLightClientFinalityUpdate CreateFromAltair(AltairLightClientUpdate pre)
    {
        return new CapellaLightClientFinalityUpdate
        {
            AttestedHeader = CapellaLightClientHeader.CreateFromAltair(pre.AttestedHeader),
            FinalizedHeader = CapellaLightClientHeader.CreateFromAltair(pre.FinalizedHeader),
            FinalityBranch = pre.FinalityBranch,
            SyncAggregate = pre.SyncAggregate,
            SignatureSlot = pre.SignatureSlot
        };
    }
    
    public static CapellaLightClientFinalityUpdate CreateDefault()
    {
        var finalityBranch = new byte[Constants.FinalityBranchDepth][];

        for (var i = 0; i < finalityBranch.Length; i++)
        {
            finalityBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(CapellaLightClientHeader.CreateDefault(), CapellaLightClientHeader.CreateDefault(), finalityBranch, AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(CapellaLightClientFinalityUpdate capellaLightClientFinalityUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<CapellaLightClientFinalityUpdate>(preset);
        var bytes = new byte[container.Length(capellaLightClientFinalityUpdate)];
        
        container.Serialize(capellaLightClientFinalityUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static CapellaLightClientFinalityUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<CapellaLightClientFinalityUpdate>(data, preset);
        return result.Item1;
    }
}