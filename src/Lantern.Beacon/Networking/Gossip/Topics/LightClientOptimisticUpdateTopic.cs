using SszSharp;

namespace Lantern.Beacon.Networking.Gossip.Topics;

public class LightClientOptimisticUpdateTopic : ITopic
{
    public string Name => "light_client_optimistic_update";
    
    public string GetTopicString(byte[] genesisValidatorsRoot, SizePreset preset)
    { 
        var forkDigest = BeaconClientUtility.GetForkDigest(genesisValidatorsRoot, preset);
        return $"/eth2/{forkDigest}/{Name}/ssz_snappy";
    }
    
    public byte[] Encode() => [];
}