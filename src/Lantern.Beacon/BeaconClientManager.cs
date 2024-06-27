using System.Collections.Concurrent;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Multiformats.Base;
using Multiformats.Hash;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClientManager : IBeaconClientManager
{
    private readonly BeaconClientOptions _clientOptions; 
        private readonly ManualDiscoveryProtocol _discoveryProtocol; 
        private readonly ICustomDiscoveryProtocol _customDiscoveryProtocol; 
        private readonly ISyncProtocol _syncProtocol; 
        private readonly IPeerFactory _peerFactory; 
        private readonly IIdentityManager _identityManager; 
        private readonly ILogger<BeaconClientManager> _logger; 
        private readonly ConcurrentQueue<Multiaddress> _peersToDial = new(); 
        private readonly ConcurrentBag<IRemotePeer> _livePeers = []; 
        private CancellationTokenSource? _cancellationTokenSource; 
        public ILocalPeer? LocalPeer { get; private set; } 

        public BeaconClientManager( 
            BeaconClientOptions clientOptions, 
            ManualDiscoveryProtocol discoveryProtocol, 
            ICustomDiscoveryProtocol customDiscoveryProtocol, 
            ISyncProtocol syncProtocol, 
            IPeerFactory peerFactory, 
            IIdentityManager identityManager, 
            ILoggerFactory loggerFactory) 
        { 
            _clientOptions = clientOptions; 
            _discoveryProtocol = discoveryProtocol; 
            _customDiscoveryProtocol = customDiscoveryProtocol; 
            _syncProtocol = syncProtocol; 
            _peerFactory = peerFactory; 
            _identityManager = identityManager; 
            _logger = loggerFactory.CreateLogger<BeaconClientManager>(); 
            _customDiscoveryProtocol.OnAddPeer = HandleDiscoveredPeer; 
        } 

        public async Task InitAsync(CancellationToken token = default) 
        { 
            try 
            { 
                var result = await _customDiscoveryProtocol.InitAsync(); 
                if (!result) 
                { 
                    _logger.LogError("Failed to start peer manager"); 
                    return; 
                } 
                var identity = new Identity(); 
                LocalPeer = _peerFactory.Create(identity); 
                LocalPeer.Address.ReplaceOrAdd<TCP>(_identityManager.Record.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value); 
                LocalPeer.Address.ReplaceOrAdd<P2P>(_identityManager.Record.ToPeerId()); 
                _logger.LogInformation("Peer manager started with address {Address}", LocalPeer.Address); 
            } 
            catch (Exception e) 
            { 
                _logger.LogError(e, "Failed to start peer manager"); 
            } 
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            if (LocalPeer == null)
            {
                throw new Exception("Local peer is not initialized. Cannot start peer manager");
            }
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            
            var syncTask = Task.Run(async () => await RunSyncProtocolAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            var peerDiscoveryTask = Task.Run(() => ProcessPeerDiscoveryAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            await Task.WhenAll(syncTask, peerDiscoveryTask);
        }

        private async Task RunSyncProtocolAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Slot: {CurrentSlot}, Head Block: {HeadRoot}, Peers: {PeerCount}", 
                    Phase0Helpers.ComputeCurrentSlot(_syncProtocol.Options.GenesisTime),
                    BitConverter.ToString(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_syncProtocol.Options.Preset).AsSpan(0, 4).ToArray()),
                    _livePeers.Count);

                await Task.Delay(Config.SecondsPerSlot * 1000, token);
            }
        }

        public async Task StopAsync()
        {
            if (_cancellationTokenSource == null)
            {
                _logger.LogWarning("Peer manager is not running. Nothing to stop");
                return;
            }
            await _cancellationTokenSource.CancelAsync();
            await _customDiscoveryProtocol.StopAsync();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private async Task ProcessPeerDiscoveryAsync(CancellationToken token)
        {
            var semaphore = new SemaphoreSlim(_clientOptions.MaxParallelDials);
            var address = Multiaddress.Decode("/ip4/34.67.74.221/tcp/9000/p2p/16Uiu2HAmTRgEakJhZkyeKJ43eJNxC1BgTpq92p237CJePnZvFSW8"); //Multiaddress.Decode("/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAmJHKsNZUo4y8U34aeVEdhBapHdtBhvb3bBS3TW7tAbNjd"); //
            await DialPeerWithThrottling(address, semaphore, token);
            await Task.Delay(3000, token);
        }

        private async Task DialPeerWithThrottling(Multiaddress peerAddress, SemaphoreSlim semaphore, CancellationToken token)
        {
            try
            {
                await DialDiscoveredPeer(peerAddress, token);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task CollectPeersToDialAsync(CancellationToken token = default)
        {
            if (LocalPeer == null)
            {
                _logger.LogError("Local peer is null. Cannot refresh peers");
                return;
            }
            await _customDiscoveryProtocol.GetDiscoveredNodesAsync(LocalPeer.Address, token);
        }

        private bool HandleDiscoveredPeer(Multiaddress[] newPeers)
        {
            if (newPeers.Length == 0)
            {
                return false;
            }
            foreach (var peer in newPeers)
            {
                _logger.LogDebug("New peer added: {Peer}", peer);
                _peersToDial.Enqueue(peer);
            }
            return true;
        }

        private async Task DialDiscoveredPeer(Multiaddress? peer, CancellationToken token = default)
        {
            if (LocalPeer == null)
            {
                _logger.LogError("Local peer is null. Cannot dial peer");
                return;
            }
            if (peer == null)
            {
                _logger.LogError("Peer's address is null. Cannot dial discovered peer");
                return;
            }
            var peerId = (Multihash)peer.Get<P2P>().Value;
            var ip4 = peer.Get<IP4>().Value.ToString();
            var tcpPort = peer.Get<TCP>().Value.ToString();
            var peerIdString = peerId.ToString(MultibaseEncoding.Base58Btc);
            try
            {
                var dialTask = LocalPeer.DialAsync(peer, token);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_clientOptions.DialTimeoutSeconds), token);
                var completedTask = await Task.WhenAny(dialTask, timeoutTask);
                
                if (completedTask != timeoutTask)
                {
                    _logger.LogInformation("Successfully dialed peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                    _livePeers.Add(dialTask.Result);
                    
                    // Only invoke if light client bootstrap initialisation is successful
                    // _discoveryProtocol.OnAddPeer?.Invoke([peer]);

                    await dialTask.Result.DialAsync<LightClientBootstrapProtocol>(token);
                    await dialTask.Result.DialAsync<LightClientUpdatesByRangeProtocol>(token);
                }
                else
                {
                    _logger.LogDebug("Dial operation timed out for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Dial task was canceled");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to dial for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
            }
        }
}