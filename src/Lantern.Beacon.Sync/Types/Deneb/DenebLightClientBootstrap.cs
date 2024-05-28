using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Deneb;

public class DenebLightClientBootstrap : IEquatable<DenebLightClientBootstrap>
{
    [SszElement(0, "Container")]
    public DenebLightClientHeader Header { get; protected init; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee CurrentSyncCommittee { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] CurrentSyncCommitteeBranch { get; protected init; } 
    
    public bool Equals(DenebLightClientBootstrap? other)
    {
        return other != null && Header.Equals(other.Header) && CurrentSyncCommittee.Equals(other.CurrentSyncCommittee) && CurrentSyncCommitteeBranch.Equals(other.CurrentSyncCommitteeBranch);
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