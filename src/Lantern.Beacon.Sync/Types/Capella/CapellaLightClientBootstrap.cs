using Cortex.Containers;
using Lantern.Beacon.Sync.Types.Altair;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Capella;

public class CapellaLightClientBootstrap : IEquatable<CapellaLightClientBootstrap>
{
    [SszElement(0, "Container")]
    public CapellaLightClientHeader Header { get; protected init; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee CurrentSyncCommittee { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] CurrentSyncCommitteeBranch { get; protected init; } 
    
    public bool Equals(CapellaLightClientBootstrap? other)
    {
        return other != null && Header.Equals(other.Header) && CurrentSyncCommittee.Equals(other.CurrentSyncCommittee) && CurrentSyncCommitteeBranch.Equals(other.CurrentSyncCommitteeBranch);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is CapellaLightClientBootstrap other)
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
        var container = SszContainer.GetContainer<CapellaLightClientBootstrap>(preset);
        return container.HashTreeRoot(this);
    }
    
    public static CapellaLightClientBootstrap CreateFromAltair(AltairLightClientBootstrap pre)
    {
        return new CapellaLightClientBootstrap
        {
            Header = CapellaLightClientHeader.CreateFromAltair(pre.Header),
            CurrentSyncCommittee = pre.CurrentSyncCommittee,
            CurrentSyncCommitteeBranch = pre.CurrentSyncCommitteeBranch
        };
    }

    public static CapellaLightClientBootstrap CreateFrom(CapellaLightClientHeader altairLightClientHeader, AltairSyncCommittee currentAltairSyncCommittee, byte[][] currentSyncCommitteeBranch)
    {
        return new CapellaLightClientBootstrap
        {
            Header = altairLightClientHeader,
            CurrentSyncCommittee = currentAltairSyncCommittee,
            CurrentSyncCommitteeBranch = currentSyncCommitteeBranch
        };
    }
    
    public static CapellaLightClientBootstrap CreateDefault()
    {
        var currentSyncCommitteeBranch = new byte[Constants.CurrentSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < currentSyncCommitteeBranch.Length; i++)
        {
            currentSyncCommitteeBranch[i] = new byte[Bytes32.Length];
        }
        
        return CreateFrom(CapellaLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), currentSyncCommitteeBranch);
    }
    
    public static byte[] Serialize(CapellaLightClientBootstrap capellaLightClientBootstrap, SizePreset preset)
    {
        var container = SszContainer.GetContainer<CapellaLightClientBootstrap>(preset);
        var bytes = new byte[container.Length(capellaLightClientBootstrap)];
        
        container.Serialize(capellaLightClientBootstrap, bytes.AsSpan());
        
        return bytes;
    }
    
    public static CapellaLightClientBootstrap Deserialize(byte[] data, SizePreset preset)
    {
        var result = SszContainer.Deserialize<CapellaLightClientBootstrap>(data, preset);
        return result.Item1;
    }
}