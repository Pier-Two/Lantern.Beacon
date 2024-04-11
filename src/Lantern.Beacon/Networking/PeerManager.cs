using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Microsoft.Extensions.Logging;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public class PeerManager(IDiscoveryProtocol discoveryProtocol, IPeerFactory peerFactory, ILoggerFactory loggerFactory) : IPeerManager
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

            LocalPeer.Address = LocalPeer.Address.Replace<IP4>(ip).Replace<TCP>(tcpPort);
            
            _logger.LogInformation("Peer manager started with address {Address}", LocalPeer.Address);
            
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start peer manager");
        }
    }
    
    public async Task StartAsync(CancellationToken token = default)
    {
        if(discoveryProtocol.SelfEnr == null)
        {
            _logger.LogError("Self ENR is null. Cannot dial peers");
            return;
        }
        
        var randomId = new byte[32];
        Random.Shared.NextBytes(randomId);

        var nodes = await discoveryProtocol.DiscoverAsync(randomId, token);

        foreach (var node in nodes)
        {
            if(node == null)
            {
                continue;
            }
            
            _logger.LogInformation("Dialing node with multi address: {Node}", node);
            
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(2)); 
            
            await DialDiscoveredNode(node, cts.Token);
        }
        
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
    
    private async Task DialDiscoveredNode(Multiaddress node, CancellationToken token)
    {
        if(LocalPeer == null)
        {
            _logger.LogError("Local peer is null. Cannot dial node");
            return;
        }
        
        try
        {
            var dialTask = LocalPeer.DialAsync(node, token);
            await Task.WhenAny(dialTask, Task.Delay(Timeout.Infinite, token));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dial task was canceled");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to dial node with multi address: {Node}", node);
        }
    }
}