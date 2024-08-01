using Lantern.Beacon.Sync;
using SszSharp;

namespace Lantern.Beacon.Networking.Gossip.Topics;

public static class LightClientFinalityUpdateTopic 
{
    private static string Name => "light_client_finality_update";
    
    public static string GetTopicString(SyncProtocolOptions options)
    { 
        var forkDigest = BeaconClientUtility.GetForkDigestString(options);
        return $"/eth2/{forkDigest}/{Name}/ssz_snappy";
    }
}