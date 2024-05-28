using System.Collections;
using Cortex.Containers;
using Lantern.Beacon.Sync.Presets.Mainnet;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Altair;

public class AltairSyncAggregate : IEquatable<AltairSyncAggregate>
{
    [SszElement(0, "Bitvector[SYNC_COMMITTEE_SIZE]")]
    public List<bool> SyncCommitteeBits { get; protected init; }
    
    [SszElement(1, "Vector[uint8, 96]")]
    public byte[] SyncCommitteeSignature { get; protected init; } 
    
    public bool Equals(AltairSyncAggregate? other)
    {
        return other != null && SyncCommitteeBits.SequenceEqual(other.SyncCommitteeBits) && SyncCommitteeSignature.SequenceEqual(other.SyncCommitteeSignature);
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
        var hash = new HashCode();

        if (SyncCommitteeBits != null)
        {
            foreach (var bit in SyncCommitteeBits)
            {
                hash.Add(bit);
            }
        }

        if (SyncCommitteeSignature != null)
        {
            foreach (var byteValue in SyncCommitteeSignature)
            {
                hash.Add(byteValue);
            }
        }

        return hash.ToHashCode();
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
        return CreateFrom(Enumerable.Repeat(false, (int)AltairPresetValues.SyncCommitteeSize).ToList(),new byte[96]);
    }
    
    public static int BytesLength => (int)AltairPresetValues.SyncCommitteeSize + BlsSignature.Length;
    
    public static byte[] Serialize(AltairSyncAggregate altairSyncAggregate, SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairSyncAggregate>(preset);
        var bytes = new byte[container.Length(altairSyncAggregate)];
        
        container.Serialize(altairSyncAggregate, bytes);

        return bytes;
    }
    
    public static AltairSyncAggregate Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<AltairSyncAggregate>(data, preset);
        return result.Item1;
    }
}