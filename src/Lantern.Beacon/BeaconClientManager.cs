using System.Collections.Concurrent;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClientManager(BeaconClientOptions clientOptions, 
    ManualDiscoveryProtocol discoveryProtocol,
    ICustomDiscoveryProtocol customDiscoveryProtocol,
    IPeerState peerState,
    ISyncProtocol syncProtocol,
    IPeerFactory peerFactory,
    IIdentityManager identityManager,
    ILoggerFactory loggerFactory) : IBeaconClientManager
{
    private readonly ILogger<BeaconClientManager> _logger = loggerFactory.CreateLogger<BeaconClientManager>();
    private readonly ConcurrentQueue<Multiaddress> _peersToDial = new(); 
    public CancellationTokenSource? CancellationTokenSource { get; private set; }
    public ILocalPeer? LocalPeer { get; private set; } 

    public async Task InitAsync(CancellationToken token = default) 
    { 
        try 
        { 
            var result = await customDiscoveryProtocol.InitAsync(); 
     
            if (!result) 
            { 
                _logger.LogError("Failed to start beacon client manager"); 
                return; 
            } 
            
            var identity = new Identity();
            
            LocalPeer = peerFactory.Create(identity); 
            LocalPeer.Address.ReplaceOrAdd<TCP>(identityManager.Record.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value);
            LocalPeer.Address.ReplaceOrAdd<P2P>(identityManager.Record.ToPeerId());
            
            if(clientOptions.Bootnodes.Length > 0) 
            { 
                foreach (var bootnode in clientOptions.Bootnodes) 
                {
                    var peerAddress = Multiaddress.Decode(bootnode); 
                    _peersToDial.Enqueue(peerAddress); 
                } 
       
                _logger.LogInformation("Added bootnodes for dialing");
            }
            
            customDiscoveryProtocol.OnAddPeer = HandleDiscoveredPeer;
            
            _logger.LogInformation("Beacon client manager started with address {Address}", LocalPeer.Address); 
        } 
        catch (Exception e) 
        { 
            _logger.LogError(e, "Failed to start beacon client manager"); 
        } 
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        if (LocalPeer == null)
        {
            throw new Exception("Local peer is not initialized. Cannot start peer manager");
        }
        
        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        var syncTask = Task.Run(async () => await DisplaySyncStatus(CancellationTokenSource.Token), CancellationTokenSource.Token);
        var peerDiscoveryTask = Task.Run(() => ProcessPeerDiscoveryAsync(CancellationTokenSource.Token), CancellationTokenSource.Token);
        var runSyncTask = Task.Run(() => MonitorPeerCountForSync(CancellationTokenSource.Token), CancellationTokenSource.Token);

        await Task.WhenAll(syncTask, peerDiscoveryTask, runSyncTask);
    }

    public async Task StopAsync()
    {
        if (CancellationTokenSource == null)
        {
            _logger.LogWarning("Peer manager is not running. Nothing to stop");
            return;
        }
        
        await CancellationTokenSource.CancelAsync();
        await customDiscoveryProtocol.StopAsync();
        
        CancellationTokenSource.Dispose();
        CancellationTokenSource = null;
    }
    
    private async Task DisplaySyncStatus(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    "Slot: {CurrentSlot}, Finalized Period: {FinalizedPeriod}, Optimistic Period: {OptimisticPeriod}, Current Period: {CurrentPeriod}, Head Block: 0x{HeadBlock}, Peer Count: {PeerCount}",
                    Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime),
                    AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot)),
                    AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot)),
                    AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime))),
                    Convert.ToHexString(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(syncProtocol.Options.Preset).AsSpan(0, 4).ToArray()).ToLower(),
                    peerState.LivePeers.Count);

                await Task.Delay(Config.SecondsPerSlot * 1000, token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Stopping sync status logging");
            }
        }
    }

    private async Task ProcessPeerDiscoveryAsync(CancellationToken token)
    {
        _logger.LogInformation("Running peer discovery...");
        var semaphore = new SemaphoreSlim(clientOptions.MaxParallelDials);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, token);

                if (peerState.LivePeers.Count >= clientOptions.TargetPeerCount)
                {
                    continue;
                }

                if (_peersToDial.IsEmpty && clientOptions.EnableDiscovery)
                {
                    if (LocalPeer == null)
                    {
                        _logger.LogError("Local peer is null. Cannot discover new peers");
                        return;
                    }
                    
                    try
                    {
                        _logger.LogInformation("Discovering more peers...");
                        await customDiscoveryProtocol.GetDiscoveredNodesAsync(LocalPeer.Address, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred during peer discovery");
                    }
                }
                else if(!_peersToDial.IsEmpty)
                {
                    var dialingTasks = new List<Task>();
                    
                    while (_peersToDial.TryDequeue(out var peerAddress))
                    {
                        await semaphore.WaitAsync(token);
                    
                        if (peerAddress == null)
                        {
                            semaphore.Release();
                            continue;
                        }
                    
                        var dialTask = DialPeerWithThrottling(peerAddress, semaphore, token);
                        dialingTasks.Add(dialTask); 
                    }

                    if (dialingTasks.Count <= 0) 
                        continue;
                
                    await Task.WhenAll(dialingTasks);
                    
                    _logger.LogInformation("Finished dialing all peers");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Stopping peer discovery");
            }
        }
    }
    
    private async Task MonitorPeerCountForSync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, token);

                if (peerState.LivePeers.IsEmpty)
                    continue;

                var peer = peerState.LivePeers.First();
                await RunSyncProtocolAsync(peer.Value, token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Stopping monitoring peer count for sync");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring peer count for sync");
            }
        }
    }

    private async Task DialPeerWithThrottling(Multiaddress peerAddress, SemaphoreSlim semaphore, CancellationToken token)
    {
        try
        {
            await DialPeer(peerAddress, token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Stopping dialing peer: {Peer}", peerAddress);
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
            _logger.LogDebug("Discovered new peer: {Peer}", peer);
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
        
        var ip4 = peer.Get<IP4>().Value.ToString();
        var tcpPort = peer.Get<TCP>().Value.ToString();
        var peerId = peer.GetPeerId();

        if(peerId == null)
        {
            _logger.LogError("Peer ID is null for address /ip4/{Ip4}/tcp/{TcpPort}. Cannot dial peer", ip4, tcpPort);
            return;
        }

        var peerIdString = peerId.ToString();

        try
        {
            _logger.LogDebug("Dialing peer at address: {PeerAddress}, {Count} peers remaining for dialing", peer, _peersToDial.Count);
            
            var dialTask = LocalPeer.DialAsync(peer, token);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(clientOptions.DialTimeoutSeconds), token);
            var completedTask = await Task.WhenAny(dialTask, timeoutTask);
            
            if (completedTask != timeoutTask)
            {
                _logger.LogInformation("Successfully dialed peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);

                var remotePeerId = dialTask.Result.Address.GetPeerId();
                var result = peerState.PeerProtocols.TryRemove(remotePeerId!, out var peerProtocols);
                
                if (!result)
                {
                    _logger.LogInformation("No protocols found for peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                    return;
                }

                var supportsLightClientProtocols = LightClientProtocols.All.All(protocol => peerProtocols!.Contains(protocol));

                if (supportsLightClientProtocols)
                {
                    _logger.LogInformation("Peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} supports all light client protocols", ip4, tcpPort, peerIdString);

                    discoveryProtocol.OnAddPeer?.Invoke([peer]);
                    peerState.LivePeers.TryAdd(peer.GetPeerId()!, dialTask.Result);
                }
                else
                {
                    _logger.LogInformation("Peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} does not support all light client protocols. Disconnecting...", ip4, tcpPort, peerIdString);
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
            if (!syncProtocol.IsInitialised)
            {
                await peer.DialAsync<LightClientBootstrapProtocol>(token);
            }

            if (!syncProtocol.IsInitialised)
            {
                _logger.LogWarning(
                    "Failed to initialize light client from peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} Disconnecting...",
                    peer.Address.Get<IP4>().Value.ToString(),
                    peer.Address.Get<TCP>().Value.ToString(),
                    peer.Address.Get<P2P>().Value.ToString());
                await peer.DialAsync<GoodbyeProtocol>(token);
            }
            else
            {
                _logger.LogInformation("Successfully initialised light client. Started syncing");
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
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Stopping sync protocol...");
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
                syncProtocol.LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(denebFinalizedPeriod, 1);

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

                syncProtocol.LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(startPeriod, count);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} and count {Count}", 
                    startPeriod, 
                    count
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
                syncProtocol.LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(capellaFinalizedPeriod, 1);

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
                    startPeriod, 
                    count
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
                syncProtocol.LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(altairFinalizedPeriod, 1);

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

                syncProtocol.LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(startPeriod, count);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} and count {Count}", 
                    startPeriod, 
                    count
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }

            await Task.Delay(1000, token);     
        }
    }
}