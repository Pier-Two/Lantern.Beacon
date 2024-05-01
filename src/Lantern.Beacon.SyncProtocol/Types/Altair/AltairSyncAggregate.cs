using System.Collections;
using Cortex.Containers;
using Nethermind.Serialization.Ssz;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairSyncAggregate : IEquatable<AltairSyncAggregate>
{
    [SszElement(0, "Bitvector[SYNC_COMMITTEE_SIZE]")]
    public List<bool> SyncCommitteeBits { get; protected init; }
    
    [SszElement(1, "Vector[uint8, 96]")]
    public byte[] SyncCommitteeSignature { get; protected init; } 
    
    public bool Equals(AltairSyncAggregate? other)
    {
        return other != null && SyncCommitteeBits.Equals(other.SyncCommitteeBits) && SyncCommitteeSignature.Equals(other.SyncCommitteeSignature);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairSyncAggregate other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(SyncCommitteeBits, SyncCommitteeSignature);
    }
    
    public static AltairSyncAggregate CreateFrom(List<bool> syncCommitteeBits, byte[] syncCommitteeSignature)
    {
        return new AltairSyncAggregate
        {
            SyncCommitteeBits = syncCommitteeBits,
            SyncCommitteeSignature = syncCommitteeSignature
        };
    }
    
    public static AltairSyncAggregate CreateDefault()
    {
        return CreateFrom(Enumerable.Repeat(false, Constants.SyncCommitteeSize).ToList(),new byte[96]);
    }
    
    public static int BytesLength => Constants.SyncCommitteeSize + BlsSignature.Length;
    
    public static byte[] Serialize(AltairSyncAggregate altairSyncAggregate)
    {
        var container = SszContainer.GetContainer<AltairSyncAggregate>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(altairSyncAggregate)];
        
        container.Serialize(altairSyncAggregate, bytes);

        return bytes;
    }
    
    public static AltairSyncAggregate Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairSyncAggregate>(data, SizePreset.MainnetPreset);
        return result.Item1;
    }
}