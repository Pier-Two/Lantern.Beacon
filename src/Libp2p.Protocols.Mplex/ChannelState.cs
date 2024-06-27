using Nethermind.Libp2p.Core;

namespace Libp2p.Protocols.Mplex;

public class ChannelState(IChannel? channel = null, IChannelRequest? request = null)
{
    public IChannel? Channel { get; } = channel;
    
    public IChannelRequest? Request { get; } = request;

    public bool IsClosedFromRemote { get; set; }
    
    public bool IsClosedFromLocal { get; set; }
}