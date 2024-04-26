using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.SimpleSerialize;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types.Altair;

public class AltairLightClientBootstrap(AltairLightClientHeader header, 
    AltairSyncCommittee currentAltairSyncCommittee,
    Bytes32[] currentSyncCommitteeBranch) : IEquatable<AltairLightClientBootstrap>
{
    public AltairLightClientHeader Header { get; init; } = header;
    
    public AltairSyncCommittee CurrentAltairSyncCommittee { get; init; } = currentAltairSyncCommittee;
    
    public Bytes32[] CurrentSyncCommitteeBranch { get; init; } = currentSyncCommitteeBranch;
    
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
    
    public static AltairLightClientBootstrap CreateDefault()
    {
        return new AltairLightClientBootstrap(AltairLightClientHeader.CreateDefault(), AltairSyncCommittee.CreateDefault(), new Bytes32[Constants.CurrentSyncCommitteeGIndex]);
    }
    
    public static int BytesLength => AltairLightClientHeader.BytesLength + AltairSyncCommittee.BytesLength + Constants.CurrentSyncCommitteeBranchDepth * Bytes32.Length;
}