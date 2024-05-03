using Cortex.Containers;
using SszSharp;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientBootstrap : IEquatable<AltairLightClientBootstrap>
{
    [SszElement(0, "Container")]
    public AltairLightClientHeader Header { get; protected init; }
    
    [SszElement(1, "Container")]
    public AltairSyncCommittee CurrentAltairSyncCommittee { get; protected init; } 
    
    [SszElement(2, "Vector[Vector[uint8, 32], 5]")]
    public byte[][] CurrentSyncCommitteeBranch { get; protected init; } 
    
    public bool Equals(AltairLightClientBootstrap? other)
    {
        return other != null && Header.Equals(other.Header) && CurrentAltairSyncCommittee.Equals(other.CurrentAltairSyncCommittee) && CurrentSyncCommitteeBranch.Equals(other.CurrentSyncCommitteeBranch);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is AltairLightClientBootstrap other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Header, CurrentAltairSyncCommittee, CurrentSyncCommitteeBranch);
    }

    public static AltairLightClientBootstrap CreateFrom(AltairLightClientHeader altairLightClientHeader, AltairSyncCommittee currentAltairSyncCommittee, byte[][] currentSyncCommitteeBranch)
    {
        return new AltairLightClientBootstrap
        {
            Header = altairLightClientHeader,
            CurrentAltairSyncCommittee = currentAltairSyncCommittee,
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
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength + Constants.CurrentSyncCommitteeBranchDepth * Bytes32.Length;
    
    public static byte[] Serialize(AltairLightClientBootstrap altairLightClientBootstrap)
    {
        var container = SszContainer.GetContainer<AltairLightClientBootstrap>(SizePreset.MainnetPreset);
        var bytes = new byte[container.Length(altairLightClientBootstrap)];
        
        container.Serialize(altairLightClientBootstrap, bytes.AsSpan());
        
        return bytes;
    }
    
    public static AltairLightClientBootstrap Deserialize(byte[] data)
    {
        var result = SszContainer.Deserialize<AltairLightClientBootstrap>(data, SizePreset.MainnetPreset);
        return result.Item1;
    }
}