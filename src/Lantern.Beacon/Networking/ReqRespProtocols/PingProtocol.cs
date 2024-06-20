using System.Buffers;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;

namespace Lantern.Beacon.Networking.ReqResp;

public class PingProtocol(IPeerManager peerManager, ILoggerFactory? loggerFactory = null) : IProtocol
{
    private readonly IPeerManager _peerManager = peerManager;
    private readonly ILogger? _logger = loggerFactory?.CreateLogger<PingProtocol>();
    public string Id => "/eth2/beacon_chain/req/ping/1/";

    public async Task DialAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {

    }

    public async Task ListenAsync(IChannel downChannel, IChannelFactory? upChannelFactory, IPeerContext context)
    {

    }
}