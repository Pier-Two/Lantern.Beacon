using System.Collections.Concurrent;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Basic;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
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

public class BeaconClientManager(BeaconClientOptions clientOptions, 
    ManualDiscoveryProtocol discoveryProtocol,
    ICustomDiscoveryProtocol customDiscoveryProtocol,
    INetworkState networkState,
    ISyncProtocol syncProtocol,
    IPeerFactory peerFactory,
    IIdentityManager identityManager,
    ILoggerFactory loggerFactory) : IBeaconClientManager
{
    private readonly ILogger<BeaconClientManager> _logger = loggerFactory.CreateLogger<BeaconClientManager>();
    private readonly ConcurrentQueue<Multiaddress> _peersToDial = new(); 
    private readonly ConcurrentBag<IRemotePeer> _livePeers = []; 
    private CancellationTokenSource? _cancellationTokenSource; 
    public ILocalPeer? LocalPeer { get; private set; } 

    public async Task InitAsync(CancellationToken token = default) 
    { 
        try 
        { 
            var result = await customDiscoveryProtocol.InitAsync(); 
            
            if (!result) 
            { 
                _logger.LogError("Failed to start peer manager"); 
                return; 
            } 
            
            var identity = new Identity(); 
            
            LocalPeer = peerFactory.Create(identity); 
            LocalPeer.Address.ReplaceOrAdd<TCP>(identityManager.Record.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value); 
            LocalPeer.Address.ReplaceOrAdd<P2P>(identityManager.Record.ToPeerId()); 
            
            if(clientOptions.Bootnodes.Length > 0) 
            { 
                _logger.LogInformation("Adding bootnodes to the queue");
                foreach (var bootnode in clientOptions.Bootnodes) 
                { 
                    var peerAddress = Multiaddress.Decode(bootnode); 
                    _peersToDial.Enqueue(peerAddress); 
                } 
                
                _logger.LogInformation("Bootnodes added to the queue");
            }
            
            customDiscoveryProtocol.OnAddPeer = HandleDiscoveredPeer;
            
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
        
        var syncTask = Task.Run(async () => await DisplaySyncStatus(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        var peerDiscoveryTask = Task.Run(() => ProcessPeerDiscoveryAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        var runSyncTask = Task.Run(() => MonitorPeerCountForSync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        await Task.WhenAll(syncTask, peerDiscoveryTask, runSyncTask);
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null)
        {
            _logger.LogWarning("Peer manager is not running. Nothing to stop");
            return;
        }
        
        await _cancellationTokenSource.CancelAsync();
        await customDiscoveryProtocol.StopAsync();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }
    
    private async Task DisplaySyncStatus(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Slot: {CurrentSlot}, Finalized Period: {FinalizedPeriod}, Optimistic Period: {OptimisticPeriod}, Current Period: {CurrentPeriod}, Head Block: 0x{HeadBlock}, Peer Count: {PeerCount}", 
                Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime),
                AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot)),
                AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot)),
                AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime))),
                Convert.ToHexString(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(syncProtocol.Options.Preset).AsSpan(0, 4).ToArray()).ToLower(), networkState.PeerCount);

            await Task.Delay(Config.SecondsPerSlot * 1000, token);
        }
    }

    private async Task ProcessPeerDiscoveryAsync(CancellationToken token)
    {
        _logger.LogInformation("Running peer discovery...");
        var semaphore = new SemaphoreSlim(clientOptions.MaxParallelDials);

        while (!token.IsCancellationRequested)
        {
            await Task.Delay(1000, token);
            
            if (networkState.PeerCount < clientOptions.TargetPeerCount && _peersToDial.IsEmpty)
            {
                _logger.LogInformation("Discovering more peers...");

                if (LocalPeer == null)
                {
                    _logger.LogError("Local peer is null. Cannot discover new peers");
                    return;
                }

                try
                {
                    _logger.LogDebug("Requesting discovered nodes for local peer: {LocalPeerAddress}", LocalPeer.Address);
                    await customDiscoveryProtocol.GetDiscoveredNodesAsync(LocalPeer.Address, token);
                    _logger.LogDebug("Discovered nodes successfully retrieved");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during peer discovery");
                }
            }

            var dialTasks = new List<Task>();
            
            while (!token.IsCancellationRequested && !_peersToDial.IsEmpty)
            {
                while (_peersToDial.TryDequeue(out var peerAddress))
                {
                    await semaphore.WaitAsync(token);
                    
                    if (peerAddress == null) 
                        continue;
                    
                    dialTasks.Add(DialPeerWithThrottling(peerAddress, semaphore, token));
                    _logger.LogDebug("Dialing peer at address: {PeerAddress}", peerAddress);
                }
                
                try
                {
                    await Task.WhenAll(dialTasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while dialing peers");
                }

                dialTasks.Clear();
            }
        }

        _logger.LogInformation("Stopping peer discovery...");
    }
    
    private async Task MonitorPeerCountForSync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (networkState.PeerCount <= 0 || _livePeers.IsEmpty) 
                continue;
            
            var peer = _livePeers.First();
            await RunSyncProtocolAsync(peer, token);
        }
    }

    private async Task DialPeerWithThrottling(Multiaddress peerAddress, SemaphoreSlim semaphore, CancellationToken token)
    {
        try
        {
            await DialPeer(peerAddress, token);
        }
        finally
        {
            semaphore.Release();
        }
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

    private async Task DialPeer(Multiaddress? peer, CancellationToken token = default)
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
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(clientOptions.DialTimeoutSeconds), token);
            var completedTask = await Task.WhenAny(dialTask, timeoutTask);
            
            if (completedTask != timeoutTask)
            {
                _logger.LogInformation("Successfully dialed peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                
                await dialTask.Result.DialAsync<CustomIdentifyProtocol>(token);
                
                var remotePeerId = dialTask.Result.Address.GetPeerId();
                var result = networkState.PeerProtocols.TryRemove(remotePeerId, out var peerProtocols);

                if (!result)
                {
                    _logger.LogInformation("No protocols found for peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                    return;
                }
                
                var supportsLightClientProtocols = LightClientProtocols.All.All(protocol => peerProtocols.Contains(protocol));
                
                if (supportsLightClientProtocols)
                {
                    _logger.LogInformation("Peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} supports all light client protocols", ip4, tcpPort, peerIdString);
                    
                    _livePeers.Add(dialTask.Result);
                    discoveryProtocol.OnAddPeer?.Invoke([peer]);
                    networkState.IncrementPeerCount();
                }
                else
                {
                    await dialTask.Result.DialAsync<GoodbyeProtocol>(token);
                    await dialTask.Result.DisconnectAsync();
                }
            }
            else
            {
                _logger.LogDebug("Dial operation timed out for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cancelled dialing to peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to dial for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
        }
    }
    
    private async Task RunSyncProtocolAsync(IRemotePeer peer, CancellationToken token)
    {
        try
        {
            await peer.DialAsync<LightClientBootstrapProtocol>(token);

            if (syncProtocol.IsNotInitialised())
            {
                _logger.LogWarning("Failed to initialize light client. Disconnecting peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", 
                    peer.Address.Get<IP4>().Value.ToString(), peer.Address.Get<TCP>().Value.ToString(), peer.Address.Get<P2P>().Value.ToString());
                await peer.DialAsync<GoodbyeProtocol>(token);
                return;
            }
            
            _logger.LogInformation("Successfully initialised light client. Starting sync"); 
            var activeFork = syncProtocol.ActiveFork;
            
            switch (activeFork)
            {
                case ForkType.Deneb:
                    await SyncDenebForkAsync(peer, token);
                    break;
                case ForkType.Capella:
                    await SyncCapellaForkAsync(peer, token);
                    break;
                case ForkType.Bellatrix:
                    await SyncAltairForkAsync(peer, token);
                    break;
                case ForkType.Altair:
                    await SyncAltairForkAsync(peer, token);
                    break;
                case ForkType.Phase0:
                    _logger.LogWarning("Active fork is Phase0. Must be on Altair or above to run sync protocol");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while running the sync protocol for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", 
                peer.Address.Get<IP4>().Value.ToString(), 
                peer.Address.Get<TCP>().Value.ToString(), 
                peer.Address.Get<P2P>().Value.ToString());
        }
    }
    
    private async Task SyncDenebForkAsync(IRemotePeer peer, CancellationToken token = default)    
    {
        while (!token.IsCancellationRequested)    
        {
            var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
            var denebOptimisticPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot));
            var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));

            if (denebFinalizedPeriod == denebOptimisticPeriod && !DenebHelpers.IsNextSyncCommitteeKnown(syncProtocol.DenebLightClientStore))
            {
                syncProtocol.LightClientUpdatesByRangeRequest =
                    LightClientUpdatesByRangeRequest.CreateFrom(denebFinalizedPeriod, 1);

                _logger.LogInformation(
                    "Next sync committee is not known. Requesting light client updates by range for period {Period}", 
                    denebOptimisticPeriod
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }
            
            if (denebFinalizedPeriod + 1 < denebCurrentPeriod)        
            {
                var startPeriod = denebFinalizedPeriod + 1;            
                var count = denebCurrentPeriod - denebFinalizedPeriod - 1;

                syncProtocol.LightClientUpdatesByRangeRequest =
                    LightClientUpdatesByRangeRequest.CreateFrom(startPeriod, count);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} and count {Count}", 
                    startPeriod, count
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }
            
            await Task.Delay(1000, token);     
        }
    }
    
    private async Task SyncCapellaForkAsync(IRemotePeer peer, CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            var capellaFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.CapellaLightClientStore.FinalizedHeader.Beacon.Slot));
            var capellaOptimisticPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.CapellaLightClientStore.OptimisticHeader.Beacon.Slot));
            var capellaCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            
            _logger.LogInformation(
                "Finalized period: {FinalizedPeriod}, Optimistic period: {OptimisticPeriod}, Current period: {CurrentPeriod}", 
                capellaFinalizedPeriod,
                capellaOptimisticPeriod,
                capellaCurrentPeriod
            );

            if (capellaFinalizedPeriod == capellaOptimisticPeriod && !AltairHelpers.IsNextSyncCommitteeKnown(syncProtocol.AltairLightClientStore))
            {
                syncProtocol.LightClientUpdatesByRangeRequest= LightClientUpdatesByRangeRequest.CreateFrom(capellaFinalizedPeriod, 1);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} as the next sync committee is not known", 
                    capellaOptimisticPeriod
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }                

            if (capellaFinalizedPeriod + 1 < capellaCurrentPeriod)        
            {
                var startPeriod = capellaFinalizedPeriod + 1;            
                var count = capellaCurrentPeriod - capellaFinalizedPeriod - 1;

                syncProtocol.LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(startPeriod, count);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} and count {Count}", 
                    startPeriod, count
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }

            await Task.Delay(1000, token);     
        }
    }
    
    private async Task SyncAltairForkAsync(IRemotePeer peer, CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            var altairFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.Slot));
            var altairOptimisticPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.Slot));
            var altairCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));
            
            _logger.LogInformation(
                "Finalized period: {FinalizedPeriod}, Optimistic period: {OptimisticPeriod}, Current period: {CurrentPeriod}", 
                altairFinalizedPeriod,
                altairOptimisticPeriod,
                altairCurrentPeriod
            );

            if (altairFinalizedPeriod == altairOptimisticPeriod && !AltairHelpers.IsNextSyncCommitteeKnown(syncProtocol.AltairLightClientStore))
            {
                syncProtocol.LightClientUpdatesByRangeRequest =
                    LightClientUpdatesByRangeRequest.CreateFrom(altairFinalizedPeriod, 1);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} as next sync committee is not known", 
                    altairOptimisticPeriod
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }        

            if (altairFinalizedPeriod + 1 < altairCurrentPeriod)        
            {
                var startPeriod = altairFinalizedPeriod + 1;            
                var count = altairCurrentPeriod - altairFinalizedPeriod - 1;

                syncProtocol.LightClientUpdatesByRangeRequest =
                    LightClientUpdatesByRangeRequest.CreateFrom(startPeriod, count);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} and count {Count}", 
                    startPeriod, count
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }

            await Task.Delay(1000, token);     
        }
    }
}