using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Deneb;

public class DenebLightClientFinalityUpdate : IEquatable<DenebLightClientFinalityUpdate>
{
    [SszElement(0, "Container")]
    public DenebLightClientHeader AttestedHeader { get; protected init; }
    
    [SszElement(1, "Container")]
    public DenebLightClientHeader FinalizedHeader { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 6]")]
    public byte[][] FinalityBranch { get; protected init; } 
    
    [SszElement(3, "Container")]
    public AltairSyncAggregate SyncAggregate { get; protected init; } 
    
    [SszElement(4, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(DenebLightClientFinalityUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && FinalizedHeader.Equals(other.FinalizedHeader) && FinalityBranch.SequenceEqual(other.FinalityBranch) && SyncAggregate.Equals(other.SyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is DenebLightClientFinalityUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, FinalizedHeader, FinalityBranch, SyncAggregate, SignatureSlot);
    }
    
    public static DenebLightClientFinalityUpdate CreateFromCapella(CapellaLightClientFinalityUpdate pre)
    {
        return new DenebLightClientFinalityUpdate
        {
            AttestedHeader = DenebLightClientHeader.CreateFromCapella(pre.AttestedHeader),
            FinalizedHeader = DenebLightClientHeader.CreateFromCapella(pre.FinalizedHeader),
            FinalityBranch = pre.FinalityBranch,
            SyncAggregate = pre.SyncAggregate,
            SignatureSlot = pre.SignatureSlot
        };
    }
    
    public static DenebLightClientFinalityUpdate CreateFrom(
        DenebLightClientHeader denebLightClientHeader, 
        DenebLightClientHeader finalizedHeader, 
        byte[][] finalityBranch, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        if (finalityBranch.Length != Constants.FinalityBranchDepth)
        {
            throw new ArgumentException($"Finalized branch length must be {Constants.FinalityBranchDepth}");
        }
        
        return new DenebLightClientFinalityUpdate
        {
            AttestedHeader = denebLightClientHeader,
            FinalizedHeader = finalizedHeader,
            FinalityBranch = finalityBranch,
            SyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }

    
    public static DenebLightClientFinalityUpdate CreateDefault()
    {
        var finalityBranch = new byte[Constants.FinalityBranchDepth][];

        for (var i = 0; i < finalityBranch.Length; i++)
        {
            finalityBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(DenebLightClientHeader.CreateDefault(), DenebLightClientHeader.CreateDefault(), finalityBranch, AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static byte[] Serialize(DenebLightClientFinalityUpdate denebLightClientFinalityUpdate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientFinalityUpdate>(preset);
        var bytes = new byte[container.Length(denebLightClientFinalityUpdate)];
        
        container.Serialize(denebLightClientFinalityUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static DenebLightClientFinalityUpdate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<DenebLightClientFinalityUpdate>(data, preset);
        return result.Item1;
    }
}