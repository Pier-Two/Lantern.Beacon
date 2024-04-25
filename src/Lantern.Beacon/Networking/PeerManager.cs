using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Microsoft.Extensions.Logging;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Multiformats.Base;
using Multiformats.Hash;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public class PeerManager(BeaconClientOptions clientOptions, IDiscoveryProtocol discoveryProtocol, IPeerFactory peerFactory, ILoggerFactory loggerFactory) : IPeerManager
{
    private readonly ILogger<PeerManager> _logger = loggerFactory.CreateLogger<PeerManager>();
    public ILocalPeer? LocalPeer { get; private set; }

    public async Task InitAsync(CancellationToken token = default)
    {
        try
        {
            var result = await discoveryProtocol.InitAsync();

            if (!result)
            {
                _logger.LogError("Failed to start peer manager");
                return;
            }
            
            LocalPeer = peerFactory.Create(new Identity());

            if (discoveryProtocol.SelfEnr == null)
            {
                _logger.LogError("Self ENR is null");
                return;
            }
            
            var ip = discoveryProtocol.SelfEnr.GetEntry<EntryIp>(EnrEntryKey.Ip).Value;
            var tcpPort = discoveryProtocol.SelfEnr.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value;
            
            LocalPeer.Address = LocalPeer.Address.ReplaceOrAdd<IP4>(ip).ReplaceOrAdd<TCP>(tcpPort);
            
            _logger.LogInformation("Peer manager started with address {Address}", LocalPeer.Address);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start peer manager");
        }
    }
    
    public async Task StartAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(clientOptions.RefreshPeersInterval, token);
            await PeerRefreshAsync(token);
        }
    }
    
    private async Task PeerRefreshAsync(CancellationToken token = default)
    {
        if(discoveryProtocol.SelfEnr == null)
        {
            _logger.LogError("Self ENR is null. Cannot dial peers");
            return;
        }

        var randomId = new byte[32];
        Random.Shared.NextBytes(randomId);

        var nodes = await discoveryProtocol.DiscoverAsync(randomId, token);
        var dialTasks = nodes.OfType<Node>().Select(node => DialDiscoveredNode(node, token)).ToList();

        await Task.WhenAll(dialTasks);
    }
    
    public void UpdateMultiAddress(Multiaddress newAddress)
    {
        if(LocalPeer == null)
        {
            _logger.LogError("Local peer is null. Cannot update address");
            return;
        }
        
        LocalPeer.Address = newAddress;
    }
    
    private async Task DialDiscoveredNode(Node node, CancellationToken token = default)
    {
        if (LocalPeer == null)
        {
            _logger.LogError("Local peer is null. Cannot dial node");
            return;
        }
        
        if (node.Address == null)
        {
            _logger.LogError("Node address is null. Cannot dial node");
            return;
        }

        var peerId = (Multihash)node.Address.Get<P2P>().Value;
        var ip4 = node.Address.Get<IP4>().Value.ToString();
        var tcpPort = node.Address.Get<TCP>().Value.ToString();
        var peerIdString = peerId.ToString(MultibaseEncoding.Base58Btc);

        try
        {
            var dialTask = LocalPeer.DialAsync(node.Address, token);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(clientOptions.DialTimeout), token);
            var completedTask = await Task.WhenAny(dialTask, timeoutTask);

            if (completedTask != timeoutTask)
            {
                _logger.LogInformation("Dial operation completed for node: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                // Handle dial success ~ store its address in a cache list
            }
            else
            {
                _logger.LogWarning("Dial operation timed out for {Enr} with address: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", node.Enr, ip4, tcpPort, peerIdString);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dial task was canceled");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to dial {Enr} with address: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", node.Enr, ip4, tcpPort, peerIdString);
        }
    }
}