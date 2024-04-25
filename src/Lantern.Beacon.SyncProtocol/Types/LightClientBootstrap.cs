using Cortex.Containers;
using Nethermind.Serialization.Ssz;

namespace Lantern.Beacon.SyncProtocol.Types;

public class LightClientBootstrap(LightClientHeader header, 
    SyncCommittee currentSyncCommittee,
    Bytes32[] currentSyncCommitteeBranch) : IEquatable<LightClientBootstrap>
{
    public LightClientHeader Header { get; init; } = header;
    
    public SyncCommittee CurrentSyncCommittee { get; init; } = currentSyncCommittee;
    
    public Bytes32[] CurrentSyncCommitteeBranch { get; init; } = currentSyncCommitteeBranch;
    
    public bool Equals(LightClientBootstrap? other)
    {
        return other != null && Header.Equals(other.Header) && CurrentSyncCommittee.Equals(other.CurrentSyncCommittee) && CurrentSyncCommitteeBranch.Equals(other.CurrentSyncCommitteeBranch);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is LightClientBootstrap other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Header, CurrentSyncCommittee, CurrentSyncCommitteeBranch);
    }
    
    public static LightClientBootstrap CreateDefault()
    {
        return new LightClientBootstrap(LightClientHeader.CreateDefault(), SyncCommittee.CreateDefault(), new Bytes32[Constants.CurrentSyncCommitteeGIndex]);
    }
    
    public static int BytesLength => LightClientHeader.BytesLength + SyncCommittee.BytesLength + Constants.CurrentSyncCommitteeBranchDepth * Bytes32.Length;

    public static class Serializer
    {
        public static byte[] Serialize(LightClientBootstrap bootstrap)
        {
            var result = new byte[BytesLength];
            var headerBytes = LightClientHeader.Serializer.Serialize(bootstrap.Header);
            var syncCommitteeBytes = SyncCommittee.Serializer.Serialize(bootstrap.CurrentSyncCommittee);
  
            Array.Copy(headerBytes, 0, result, 0, LightClientHeader.BytesLength);
            Array.Copy(syncCommitteeBytes, 0, result, LightClientHeader.BytesLength, SyncCommittee.BytesLength);
            
            var offset = LightClientHeader.BytesLength + SyncCommittee.BytesLength;
            
            foreach (var array in bootstrap.CurrentSyncCommitteeBranch)
            {
                Ssz.Encode(result.AsSpan(offset, Bytes32.Length), (ReadOnlySpan<byte>)array);
                offset += Bytes32.Length;
            }
            
            return result;
        }
        
        public static LightClientBootstrap Deserialize(byte[] bytes)
        {
            var header = LightClientHeader.Serializer.Deserialize(bytes.AsSpan(0, LightClientHeader.BytesLength));
            var syncCommittee = SyncCommittee.Serializer.Deserialize(bytes.AsSpan(LightClientHeader.BytesLength, SyncCommittee.BytesLength));
            var branch = new Bytes32[Constants.CurrentSyncCommitteeBranchDepth];
            var offset = LightClientHeader.BytesLength + SyncCommittee.BytesLength;
            
            for (var i = 0; i < Constants.CurrentSyncCommitteeBranchDepth; i++)
            {
                branch[i] = new Bytes32(Ssz.DecodeBytes32(bytes.AsSpan(offset, Bytes32.Length)).AsSpan());
                offset += Bytes32.Length;
            }
            
            return new LightClientBootstrap(header, syncCommittee, branch);
        }
    }
}