using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Mplex;

public class ChannelState(IChannel? channel = null, IChannelRequest? request = null)
{
    public IChannel? Channel { get; } = channel;
    
    public IChannelRequest? Request { get; } = request;
    
    public bool IsClosed { get; set; }
}