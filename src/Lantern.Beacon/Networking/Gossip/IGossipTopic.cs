using SszSharp;

namespace Lantern.Beacon.Networking.Gossip;

public interface IGossipTopic
{
    string Name { get; }
    
    string GetTopicString(byte[] genesisValidatorsRoot, SizePreset preset);

    byte[] Encode();
}