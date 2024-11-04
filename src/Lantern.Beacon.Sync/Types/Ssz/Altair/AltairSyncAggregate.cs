using System.Text.Json.Serialization;
using Lantern.Beacon.Sync.Presets.Mainnet;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Altair;

public class AltairSyncAggregate : IEquatable<AltairSyncAggregate>
{
    [JsonPropertyName("sync_committee_bits")]
    public string SyncCommitteeBitsJson => ConvertBitsToHexString(SyncCommitteeBits);
    
    [JsonPropertyName("sync_committee_signature")]
    public string SyncCommitteeSignatureJson => $"0x{BitConverter.ToString(SyncCommitteeSignature).Replace("-", "").ToLower()}";
    
    [JsonIgnore] 
    [SszElement(0, "Bitvector[SYNC_COMMITTEE_SIZE]")]
    public List<bool> SyncCommitteeBits { get; protected init; }
    
    [JsonIgnore] 
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
        return CreateFrom(Enumerable.Repeat(false, AltairPresetValues.SyncCommitteeSize).ToList(),new byte[96]);
    }
    
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
    
    private static string ConvertBitsToHexString(List<bool> bits)
    {
        var byteCount = (bits.Count + 7) / 8; 
        var bytes = new byte[byteCount];

        for (var i = 0; i < bits.Count; i++)
        {
            if (bits[i])
            {
                bytes[i / 8] |= (byte)(1 << (i % 8));
            }
        }
        
        Array.Reverse(bytes);

        return $"0x{BitConverter.ToString(bytes).Replace("-", "").ToLower()}";
    }
}