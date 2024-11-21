using System.Collections.Concurrent;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon;

public class BeaconClientManager(
    BeaconClientOptions clientOptions,
    ManualDiscoveryProtocol discoveryProtocol,
    ILiteDbService liteDbService,
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

    public async Task InitAsync()
    {
        try
        {
            var result = await customDiscoveryProtocol.InitAsync();

            if (!result)
            {
                _logger.LogError("Failed to start beacon client manager");
                return;
            }
            
            LocalPeer = peerFactory.Create(clientOptions.Identity);
            LocalPeer.Address.ReplaceOrAdd<TCP>(identityManager.Record.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value);
            LocalPeer.Address.ReplaceOrAdd<P2P>(identityManager.Record.ToPeerId());

            if (clientOptions.Bootnodes.Count > 0)
            {
                foreach (var peerAddress in clientOptions.Bootnodes.Select(Multiaddress.Decode))
                {
                    _peersToDial.Enqueue(peerAddress);
                }

                _logger.LogInformation("Added {Count} bootnodes for dialing", clientOptions.Bootnodes.Count);
            }

            var storedPeers = liteDbService.FetchAll<MultiAddressStore>(nameof(MultiAddressStore));

            var peers = 0;

            foreach (var storedPeer in storedPeers)
            {
                var peerAddress = Multiaddress.Decode(storedPeer.MultiAddress);

                if (_peersToDial.Contains(peerAddress))
                {
                    continue;
                }

                _peersToDial.Enqueue(peerAddress);
                peers++;
            }

            if (peers > 0)
            {
                _logger.LogInformation("Added {Count} peers for dialing from local storage", peers);
            }

            customDiscoveryProtocol.OnAddPeer = HandleDiscoveredPeer;

            _logger.LogInformation("Client started with Libp2p address {Address}", LocalPeer.Address);
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
            throw new Exception("Local peer is not initialised. Cannot start peer manager");
        }

        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

        var syncTask = Task.Run(async() => await DisplaySyncStatus(CancellationTokenSource.Token),
            CancellationTokenSource.Token);
        var peerDiscoveryTask = Task.Run(() => ProcessPeerDiscoveryAsync(CancellationTokenSource.Token),
            CancellationTokenSource.Token);
        var runSyncTask = Task.Run(() => MonitorSyncStatus(CancellationTokenSource.Token),
            CancellationTokenSource.Token);

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
                    "Slot: {CurrentSlot}, Epoch: {CurrentEpoch}, Finalized Period: {FinalizedPeriod}, Optimistic Period: {OptimisticPeriod}, Current Period: {CurrentPeriod}, Head Block: 0x{HeadBlock}, Bootstrap Peers: {BootstrapPeers}, Gossip Peers: {GossipPeers}",
                    Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime),
                    Phase0Helpers.ComputeEpochAtSlot(
                        Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)),
                    AltairHelpers.ComputeSyncCommitteePeriod(
                        Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon
                            .Slot)),
                    AltairHelpers.ComputeSyncCommitteePeriod(
                        Phase0Helpers.ComputeEpochAtSlot(
                            syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot)),
                    AltairHelpers.ComputeSyncCommitteePeriod(
                        Phase0Helpers.ComputeEpochAtSlot(
                            Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime))),
                    Convert.ToHexString(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon
                        .GetHashTreeRoot(syncProtocol.Options.Preset).AsSpan(0, 4).ToArray()).ToLower(),
                    peerState.BootstrapPeers.Count,
                    peerState.GossipPeers.Count);

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
        _logger.LogDebug("Running peer discovery...");
        var semaphore = new SemaphoreSlim(clientOptions.MaxParallelDials);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, token);
                
                if (peerState.GossipPeers.Count >= clientOptions.TargetPeerCount)
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
                else if (!_peersToDial.IsEmpty)
                {
                    var dialingTasks = new List<Task>();
                    var pendingPeersToDial = new ConcurrentQueue<Multiaddress>();

                    while (_peersToDial.TryDequeue(out var peerAddress))
                    {
                        if (peerState.GossipPeers.Count >= clientOptions.TargetPeerCount)
                        {
                            pendingPeersToDial.Enqueue(peerAddress);
                            continue;
                        }

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

                    while (!pendingPeersToDial.IsEmpty && !_peersToDial.IsEmpty)
                    {
                        pendingPeersToDial.TryDequeue(out var peerAddress);

                        if (peerAddress != null)
                        {
                            _peersToDial.Enqueue(peerAddress);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Stopping peer discovery");
            }
        }
    }

    private async Task DialPeerWithThrottling(Multiaddress peerAddress, SemaphoreSlim semaphore,
        CancellationToken token)
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

    private async Task DialPeer(Multiaddress peer, CancellationToken token = default)
    {
        if (LocalPeer == null)
        {
            _logger.LogError("Local peer is null. Cannot dial peer");
            return;
        }

        var ip4 = peer.Get<IP4>().Value.ToString();
        var tcpPort = peer.Get<TCP>().Value.ToString();
        var peerId = peer.GetPeerId();

        if (peerId == null)
        {
            _logger.LogError("Peer ID is null for address /ip4/{Ip4}/tcp/{TcpPort}. Cannot dial peer", ip4, tcpPort);
            return;
        }

        var peerIdString = peerId.ToString();

        try
        {
            _logger.LogDebug("Dialing peer at address: {PeerAddress}, {Count} peers remaining for dialing", peer,
                _peersToDial.Count);

            var dialTask = LocalPeer.DialAsync(peer, token);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(clientOptions.DialTimeoutSeconds), token);
            var completedTask = await Task.WhenAny(dialTask, timeoutTask);

            if (completedTask != timeoutTask)
            {
                _logger.LogDebug("Successfully dialed peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort,
                    peerIdString);

                var remotePeerId = dialTask.Result.Address.GetPeerId();
                var result = peerState.PeerProtocols.TryRemove(remotePeerId!, out var peerProtocols);

                if (!result)
                {
                    _logger.LogDebug("No protocols found for peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4,
                        tcpPort, peerIdString);
                    await dialTask.Result.DisconnectAsync();
                    return;
                }

                var supportsLightClientProtocols = LightClientProtocols.All.All(protocol => peerProtocols!.Contains(protocol));

                if (supportsLightClientProtocols)
                {
                    _logger.LogInformation(
                        "Found peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} that supports all light client protocols", ip4, tcpPort,
                        peerIdString);

                    discoveryProtocol.OnAddPeer?.Invoke([peer]);
                    var peerAddResult = peerState.BootstrapPeers.TryAdd(peer.GetPeerId()!, dialTask.Result);

                    if (peerAddResult)
                    {
                        var multiAddressStore = new MultiAddressStore
                        {
                            MultiAddress = peer.ToString()
                        };

                        liteDbService.ReplaceByPredicate(
                            nameof(MultiAddressStore),
                            x => x.MultiAddress.Equals(multiAddressStore.MultiAddress),
                            multiAddressStore
                        );
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} does not support all light client protocols. Disconnecting...",
                        ip4, tcpPort, peerIdString);
                    await dialTask.Result.DisconnectAsync();
                }
            }
            else
            {
                _logger.LogDebug("Dial operation timed out for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4,
                    tcpPort, peerIdString);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cancelled dialing to peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort,
                peerIdString);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to dial for peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort,
                peerIdString);
        }
    }
    
    private async Task MonitorSyncStatus(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!peerState.BootstrapPeers.IsEmpty)
                {
                    var peer = peerState.BootstrapPeers.Values.ElementAt(new Random().Next(peerState.BootstrapPeers.Count));

                    if (!syncProtocol.IsInitialised)
                    {
                        _logger.LogInformation(
                            "Requesting light client bootstrap from peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}",
                            peer.Address.Get<IP4>().Value.ToString(),
                            peer.Address.Get<TCP>().Value.ToString(),
                            peer.Address.Get<P2P>().Value.ToString());

                        await DialPeerWithProtocol<LightClientBootstrapProtocol>(peer, token);
                    }

                    if (!syncProtocol.IsInitialised)
                    {
                        _logger.LogWarning(
                            "Failed to initialize light client from peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}. Disconnecting...",
                            peer.Address.Get<IP4>().Value.ToString(),
                            peer.Address.Get<TCP>().Value.ToString(),
                            peer.Address.Get<P2P>().Value.ToString());
                        
                        peerState.BootstrapPeers.TryRemove(peer.Address.GetPeerId()!, out _);
                        await DialPeerWithProtocol<GoodbyeProtocol>(peer, token);
                    }
                    else
                    {
                        var activeFork = syncProtocol.ActiveFork;

                        switch (activeFork)
                        {
                            case ForkType.Deneb:
                                await SyncDenebForkAsync(peer, token);
                                break;
                            case ForkType.Phase0:
                                _logger.LogWarning(
                                    "Active fork is Phase0. Must be on Altair or above to run sync protocol");
                                break;
                        }
                    }
                }

                await Task.Delay(1000, token);
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
    
    private async Task SyncDenebForkAsync(IRemotePeer peer, CancellationToken token = default)
    {
        if (!clientOptions.GossipSubEnabled)
        {
            var tokenSource = new CancellationTokenSource();
            _ = RunOptimisticUpdateLoopAsync(peer, tokenSource.Token);
            _ = RunFinalityUpdateLoopAsync(peer, tokenSource.Token);
        }

        var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
        var denebOptimisticPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot));
        var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(
            Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime)));

        if (denebFinalizedPeriod == denebOptimisticPeriod &&
            !DenebHelpers.IsNextSyncCommitteeKnown(syncProtocol.DenebLightClientStore))
        {
            syncProtocol.LightClientUpdatesByRangeRequest =
                LightClientUpdatesByRangeRequest.CreateFrom(denebFinalizedPeriod, 1);

            _logger.LogInformation(
                "Next sync committee is not known. Requesting light client updates by range for period {Period}",
                denebOptimisticPeriod
            );

            var result = await DialPeerWithProtocol<LightClientUpdatesByRangeProtocol>(peer, token);
            
            if (!result)
            {
                peerState.BootstrapPeers.TryRemove(peer.Address.GetPeerId()!, out _);
            }
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

            var result = await DialPeerWithProtocol<LightClientUpdatesByRangeProtocol>(peer, token);

            if (!result)
            {
                peerState.BootstrapPeers.TryRemove(peer.Address.GetPeerId()!, out _);
            }
        }
    }
    
    private async Task<bool> DialPeerWithProtocol<T>(IRemotePeer peer, CancellationToken token = default) where T : IProtocol
    {
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(clientOptions.DialTimeoutSeconds), token);
        var dialTask = peer.DialAsync<T>(token);

        var completedTask = await Task.WhenAny(dialTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            return false;
        }
        
        await dialTask;
        return true;
    }

    private async Task RunOptimisticUpdateLoopAsync(IRemotePeer peer, CancellationToken token)
    {
        try
        {
            var genesisTime = syncProtocol.Options.GenesisTime;
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(genesisTime);
            var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nextSlotStartTime = genesisTime + (currentSlot + 1) * (ulong)Config.SecondsPerSlot;
            var delayUntilNextSlot = nextSlotStartTime - currentTime;

            _logger.LogInformation("Delaying for {DelayUntilNextSlot} seconds until the next slot starts...",
                delayUntilNextSlot);

            await Task.Delay((int)delayUntilNextSlot * 1000, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!syncProtocol.IsInitialised)
                {
                    _logger.LogInformation("Sync protocol is not initialised. Skipping optimistic update loop");
                    continue;
                }

                var optimisticHeadSlot = syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot;
                var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);

                if (optimisticHeadSlot >= currentSlot - 2)
                    continue;

                _logger.LogInformation("Requesting optimistic update...");

                var result = await DialPeerWithProtocol<LightClientOptimisticUpdateProtocol>(peer, token);
                
                if (!result)
                {
                    break;
                }

                await Task.Delay(Config.SecondsPerSlot * 1000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running optimistic update loop");
            }
        }
    }

    private async Task RunFinalityUpdateLoopAsync(IRemotePeer peer, CancellationToken token)
    {
        try
        {
            var genesisTime = syncProtocol.Options.GenesisTime;
            var currentSlot = Phase0Helpers.ComputeCurrentSlot(genesisTime);
            var currentEpoch = Phase0Helpers.ComputeEpochAtSlot(currentSlot);
            var secondsPerSlot = (ulong)Config.SecondsPerSlot;
            var nextEpochStartTime = genesisTime + (currentEpoch + 1) * 32 * secondsPerSlot;
            var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var delayUntilNextEpoch = nextEpochStartTime - currentTime;

            _logger.LogInformation($"Delaying for {delayUntilNextEpoch} seconds until the next epoch starts...");

            await Task.Delay((int)delayUntilNextEpoch * 1000, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!syncProtocol.IsInitialised)
                    continue;

                var finalizedHeadSlot = syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot;
                var finalizedHeadEpoch = Phase0Helpers.ComputeEpochAtSlot(finalizedHeadSlot);
                var currentSlot = Phase0Helpers.ComputeCurrentSlot(syncProtocol.Options.GenesisTime);
                var currentEpoch = Phase0Helpers.ComputeEpochAtSlot(currentSlot);

                if (finalizedHeadEpoch >= currentEpoch - 2)
                    continue;

                _logger.LogInformation("Requesting finality update...");
                
                var result = await DialPeerWithProtocol<LightClientFinalityUpdateProtocol>(peer, token);
                
                if (!result)
                {
                    break;
                }

                await Task.Delay(Config.SecondsPerSlot * 32 * 1000, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running finality update loop");
            }
        }
    }
}