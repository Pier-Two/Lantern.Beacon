using SszSharp;

namespace Lantern.Beacon.Networking;

public interface ITopic
{
    string Name { get; }
    
    string GetTopicString(byte[] genesisValidatorsRoot, SizePreset preset);

    byte[] Encode();
}