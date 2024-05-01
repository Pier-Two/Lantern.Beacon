using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientOptimisticUpdate : IEquatable<AltairLightClientOptimisticUpdate>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader AttestedHeader { get; protected init; } 
    
    [SszElement(1, "Container")]
    public AltairSyncAggregate AltairSyncAggregate { get; protected init; } 
    
    [SszElement(2, "uint64")]
    public ulong SignatureSlot { get; protected init; } 
    
    public bool Equals(AltairLightClientOptimisticUpdate? other)
    {
        return other != null && AttestedHeader.Equals(other.AttestedHeader) && AltairSyncAggregate.Equals(other.AltairSyncAggregate) && SignatureSlot.Equals(other.SignatureSlot);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientOptimisticUpdate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(AttestedHeader, AltairSyncAggregate, SignatureSlot);
    }
    
    public static AltairLightClientOptimisticUpdate CreateFrom(
        AltairLightClientHeader altairLightClientHeader, 
        AltairSyncAggregate altairSyncAggregate, 
        ulong signatureSlot)
    {
        return new AltairLightClientOptimisticUpdate
        {
            AttestedHeader = altairLightClientHeader,
            AltairSyncAggregate = altairSyncAggregate,
            SignatureSlot = signatureSlot
        };
    }
    
    public static AltairLightClientOptimisticUpdate CreateDefault()
    {
        return AltairLightClientOptimisticUpdate.CreateFrom(AltairLightClientHeader.CreateDefault(), AltairSyncAggregate.CreateDefault(), 0);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairSyncAggregate.BytesLength + sizeof(ulong);
    
    public static byte[] Serialize(AltairLightClientOptimisticUpdate altairLightClientOptimisticUpdate)
    {
        var container = SszContainer.GetContainer<AltairLightClientOptimisticUpdate>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(altairLightClientOptimisticUpdate)];
        
        container.Serialize(altairLightClientOptimisticUpdate, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientOptimisticUpdate Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairLightClientOptimisticUpdate>(data, SizePreset.MainnetPreset);
        return result.Item1;
    }
}