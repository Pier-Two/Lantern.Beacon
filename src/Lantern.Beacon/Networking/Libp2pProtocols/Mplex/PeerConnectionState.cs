using System.Collections.Concurrent;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Mplex;

public class PeerConnectionState
{
    // For storing streams where self is the initiator
    public ConcurrentDictionary<long, ChannelState> InitiatorChannels { get; } = new();
    
    // For storing streams where self is not the initiator
    public ConcurrentDictionary<long, ChannelState> ReceiverChannels { get; } = new();
    
    public long StreamIdCounter;
}