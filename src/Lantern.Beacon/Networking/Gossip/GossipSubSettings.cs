using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Presets;
using Nethermind.Libp2p.Protocols.Pubsub;

namespace Lantern.Beacon.Networking.Gossip;

public static class GossipSubSettings
{
    public static int Degree { get; set; } = 8;
    
    public static int LowestDegree { get; set; } = 6;
    
    public static int HighestDegree { get; set; } = 12;
    
    public static int LazyDegree { get; set; } = 6;
    
    public static int HeartbeatInterval { get; set; } = 7 * 100;
    
    public static int FanoutTtl { get; set; } = 60 * 1000;

    public static int MCacheLen { get; set; } = 6;
    
    public static int MCacheGossip { get; set; } = 3;
    
    public static int MessageCacheTtl { get; set; } = Config.SecondsPerSlot * Phase0Preset.SlotsPerEpoch * 2;
    
    public static Settings.SignaturePolicy DefaultSignaturePolicy { get; set; } = Settings.SignaturePolicy.StrictNoSign;
}