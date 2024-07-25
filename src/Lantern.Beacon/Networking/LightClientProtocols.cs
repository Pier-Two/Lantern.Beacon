namespace Lantern.Beacon.Networking;

public static class LightClientProtocols
{
    private const string LightClientBootstrap = "/eth2/beacon_chain/req/light_client_bootstrap/1/ssz_snappy";
    private const string LightClientFinalityUpdate = "/eth2/beacon_chain/req/light_client_finality_update/1/ssz_snappy";
    private const string LightClientOptimisticUpdate = "/eth2/beacon_chain/req/light_client_optimistic_update/1/ssz_snappy";
    private const string LightClientUpdatesByRange = "/eth2/beacon_chain/req/light_client_updates_by_range/1/ssz_snappy";

    public static readonly IReadOnlyList<string> All = new List<string>
    {
        LightClientBootstrap,
        LightClientFinalityUpdate,
        LightClientOptimisticUpdate,
        LightClientUpdatesByRange
    };
}