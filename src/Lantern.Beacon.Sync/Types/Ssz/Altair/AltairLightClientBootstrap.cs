using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Altair;

public class AltairLightClientBootstrap : IEquatable<AltairLightClientBootstrap>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader Header { get; protected init; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee CurrentSyncCommittee { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] CurrentSyncCommitteeBranch { get; protected init; } 
    
    public bool Equals(AltairLightClientBootstrap? other)
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
        if (obj is AltairLightClientBootstrap other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public byte[] GetHashTreeRoot(SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairLightClientBootstrap>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static byte[] GetHashTreeRoot(SizePreset preset, AltairLightClientBootstrap altairLightClientBootstrap)
    {
        var container = SszContainer.GetContainer<AltairLightClientBootstrap>(preset);
        return container.HashTreeRoot(altairLightClientBootstrap);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Header, CurrentSyncCommittee, CurrentSyncCommitteeBranch);
    }

    public static AltairLightClientBootstrap CreateFrom(AltairLightClientHeader altairLightClientHeader, AltairSyncCommittee currentAltairSyncCommittee, byte[][] currentSyncCommitteeBranch)
    {
        return new AltairLightClientBootstrap
        {
            Header = altairLightClientHeader,
            CurrentSyncCommittee = currentAltairSyncCommittee,
            CurrentSyncCommitteeBranch = currentSyncCommitteeBranch
        };
    }
    
    public static AltairLightClientBootstrap CreateDefault()
    {
        var currentSyncCommitteeBranch = new byte[Constants.CurrentSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < currentSyncCommitteeBranch.Length; i++)
        {
            currentSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), currentSyncCommitteeBranch);
    }
    
    public static byte[] Serialize(AltairLightClientBootstrap altairLightClientBootstrap, SizePreset preset)
    {
        var container = SszContainer.GetContainer<AltairLightClientBootstrap>(preset);
        var bytes = new byte[container.Length(altairLightClientBootstrap)];
        
        container.Serialize(altairLightClientBootstrap, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientBootstrap Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<AltairLightClientBootstrap>(data, preset);
        return result.Item1;
    }
}