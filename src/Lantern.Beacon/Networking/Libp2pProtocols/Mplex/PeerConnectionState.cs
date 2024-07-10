using System.Collections.Concurrent;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Mplex;

public class PeerConnectionState
{
    public ConcurrentDictionary<long, ChannelState> InitiatorChannels { get; } = new();
    
    public ConcurrentDictionary<long, ChannelState> ReceiverChannels { get; } = new();
    
    public long StreamIdCounter;
}