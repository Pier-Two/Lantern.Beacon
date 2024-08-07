using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Deneb;

public class DenebLightClientBootstrap : IEquatable<DenebLightClientBootstrap>
{
    [SszElement(0, "Container")]
    public DenebLightClientHeader Header { get; private init; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee CurrentSyncCommittee { get; private init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] CurrentSyncCommitteeBranch { get; private init; }
    
    public bool Equals(DenebLightClientBootstrap? other)
    {
        if (other == null) return false;
        
        if (!Header.Equals(other.Header) || !CurrentSyncCommittee.Equals(other.CurrentSyncCommittee))
        {
            return false;
        }
        
        if (CurrentSyncCommitteeBranch.Length != other.CurrentSyncCommitteeBranch.Length)
        {
            return false;
        }

        for (var i = 0; i < CurrentSyncCommitteeBranch.Length; i++)
        {
            if (!CurrentSyncCommitteeBranch[i].SequenceEqual(other.CurrentSyncCommitteeBranch[i]))
            {
                return false;
            }
        }

        return true;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is DenebLightClientBootstrap other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Header, CurrentSyncCommittee, CurrentSyncCommitteeBranch);
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientBootstrap>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static byte[] GetHashTreeRoot(SizePreset preset, DenebLightClientBootstrap denebLightClientBootstrap)
    {
        var container = SszContainer.GetContainer<DenebLightClientBootstrap>(preset);
        return container.HashTreeRoot(denebLightClientBootstrap);
    }
    
    public static DenebLightClientBootstrap CreateFromCapella(CapellaLightClientBootstrap pre)
    {
        return new DenebLightClientBootstrap
        {
            Header = DenebLightClientHeader.CreateFromCapella(pre.Header),
            CurrentSyncCommittee = pre.CurrentSyncCommittee,
            CurrentSyncCommitteeBranch = pre.CurrentSyncCommitteeBranch
        };
    }

    public static DenebLightClientBootstrap CreateFrom(DenebLightClientHeader altairLightClientHeader, AltairSyncCommittee currentAltairSyncCommittee, byte[][] currentSyncCommitteeBranch)
    {
        return new DenebLightClientBootstrap
        {
            Header = altairLightClientHeader,
            CurrentSyncCommittee = currentAltairSyncCommittee,
            CurrentSyncCommitteeBranch = currentSyncCommitteeBranch
        };
    }
    
    public static DenebLightClientBootstrap CreateDefault()
    {
        var currentSyncCommitteeBranch = new byte[Constants.CurrentSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < currentSyncCommitteeBranch.Length; i++)
        {
            currentSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(DenebLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), currentSyncCommitteeBranch);
    }
    
    public static byte[] Serialize(DenebLightClientBootstrap denebLightClientBootstrap, SizePreset preset)
    {
        var container = SszContainer.GetContainer<DenebLightClientBootstrap>(preset);
        var bytes = new byte[container.Length(denebLightClientBootstrap)];
        
        container.Serialize(denebLightClientBootstrap, bytes.AsSpan());
        
        return bytes;
    }
    
    public static DenebLightClientBootstrap Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<DenebLightClientBootstrap>(data, preset);
        return result.Item1;
    }
}