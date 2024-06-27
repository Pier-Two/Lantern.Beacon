using Lantern.Beacon.Sync;
using SszSharp;

namespace Lantern.Beacon.Networking.Gossip.Topics;

public static class BeaconBlockTopic
{
    public static string Name => "beacon_block";
    
    public static string GetTopicString(SyncProtocolOptions options)
    { 
        var forkDigest = BeaconClientUtility.GetForkDigestString(options);
        return $"/eth2/{forkDigest}/{Name}/ssz_snappy";
    }
}