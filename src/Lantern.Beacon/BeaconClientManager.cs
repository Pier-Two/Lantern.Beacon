using System.Collections.Concurrent;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Lantern.Beacon.Networking.ReqRespProtocols;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Deneb;
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
                "Slot: {CurrentSlot}, Finalized Period: {FinalizedPeriod}, Optimistic Period: {OptimisticPeriod}, Current Period: {CurrentPeriod}, Head Block: 0x{HeadBlock}, Peer Count: {PeerCount}", 
                Phase0Helpers.ComputeCurrentSlot(_syncProtocol.Options.GenesisTime),
                AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot)),
                AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot)),
                AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(_syncProtocol.Options.GenesisTime))),
                Convert.ToHexString(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_syncProtocol.Options.Preset).AsSpan(0, 4).ToArray()).ToLower(),
                _syncProtocol.PeerCount);

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
        var address = Multiaddress.Decode("/ip4/34.67.74.221/tcp/9000/p2p/16Uiu2HAmTRgEakJhZkyeKJ43eJNxC1BgTpq92p237CJePnZvFSW8");  //Multiaddress.Decode("/ip4/0.0.0.0/tcp/9012/p2p/16Uiu2HAmJHKsNZUo4y8U34aeVEdhBapHdtBhvb3bBS3TW7tAbNjd"); //
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
                
                await dialTask.Result.DialAsync<CustomIdentifyProtocol>(token);
                
                var remotePeerId = dialTask.Result.Address.GetPeerId();
                var result = _syncProtocol.PeerProtocols.TryRemove(remotePeerId, out var peerProtocols);

                if (!result)
                {
                    _logger.LogInformation("No protocols found for peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", ip4, tcpPort, peerIdString);
                    return;
                }
                
                var supportsLightClientProtocols = LightClientProtocols.All.All(protocol => peerProtocols.Contains(protocol));
                
                if (supportsLightClientProtocols)
                {
                    _livePeers.Add(dialTask.Result);
                    _discoveryProtocol.OnAddPeer?.Invoke([peer]);
                    _syncProtocol.PeerCount++;
                    _logger.LogInformation("Peer /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId} supports all light client protocols", ip4, tcpPort, peerIdString);
                    await RunSyncProtocolAsync(dialTask.Result, token);
                }
                else
                {
                    await dialTask.Result.DialAsync<GoodbyeProtocol>(token);
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

            if (!_syncProtocol.IsInitialized)
            {
                _logger.LogWarning("Failed to initialize light client. Closing connection with peer: /ip4/{Ip4}/tcp/{TcpPort}/p2p/{PeerId}", 
                    peer.Address.Get<IP4>().Value.ToString(), 
                    peer.Address.Get<TCP>().Value.ToString(), 
                    peer.Address.Get<P2P>().Value.ToString());
                await peer.DialAsync<GoodbyeProtocol>(token);
                return;
            }
            
            _logger.LogInformation("Successfully initialised light client from bootstrap. Running the sync protocol"); 
            var activeFork = _syncProtocol.ActiveFork;
            
            switch (activeFork)
            {
                case ForkType.Deneb:
                    await SyncDenebForkAsync(peer, token);
                    break;
                case ForkType.Capella:
                    await SyncCapellaForkAsync(peer, token);
                    break;
                case ForkType.Bellatrix:
                    
                    break;
                case ForkType.Altair:
                    
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
    
    public async Task SyncDenebForkAsync(IRemotePeer peer, CancellationToken token = default)    
    {
        while (!token.IsCancellationRequested)    
        {
            var denebFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot));
            var denebOptimisticPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot));
            var denebCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(_syncProtocol.Options.GenesisTime)));

            if (denebFinalizedPeriod == denebOptimisticPeriod && !DenebHelpers.IsNextSyncCommitteeKnown(_syncProtocol.DenebLightClientStore))        
            {
                _syncProtocol.SetLightClientUpdatesByRangeRequest(denebFinalizedPeriod, 1);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} as next sync committee is not known", 
                    denebOptimisticPeriod
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }
            
            if (denebFinalizedPeriod + 1 < denebCurrentPeriod)        
            {
                var startPeriod = denebFinalizedPeriod + 1;            
                var count = denebCurrentPeriod - denebFinalizedPeriod - 1;

                _syncProtocol.SetLightClientUpdatesByRangeRequest(startPeriod, count);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} and count {Count}", 
                    startPeriod, count
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }
            
            await Task.Delay(1000, token);     
        }
    }
    
    public async Task SyncCapellaForkAsync(IRemotePeer peer, CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            var capellaFinalizedPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(_syncProtocol.CapellaLightClientStore.FinalizedHeader.Beacon.Slot));
            var capellaOptimisticPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(_syncProtocol.CapellaLightClientStore.OptimisticHeader.Beacon.Slot));
            var capellaCurrentPeriod = AltairHelpers.ComputeSyncCommitteePeriod(Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(_syncProtocol.Options.GenesisTime)));
            
            _logger.LogInformation(
                "Finalized period: {FinalizedPeriod}, Optimistic period: {OptimisticPeriod}, Current period: {CurrentPeriod}", 
                capellaFinalizedPeriod,
                capellaOptimisticPeriod,
                capellaCurrentPeriod
            );

            if (capellaFinalizedPeriod == capellaOptimisticPeriod && !DenebHelpers.IsNextSyncCommitteeKnown(_syncProtocol.DenebLightClientStore))        
            {
                _syncProtocol.SetLightClientUpdatesByRangeRequest(capellaFinalizedPeriod, 1);

                _logger.LogInformation(
                    "Requesting light client updates by range for period {Period} as next sync committee is not known", 
                    capellaOptimisticPeriod
                );

                await peer.DialAsync<LightClientUpdatesByRangeProtocol>(token);        
            }                

            if (capellaFinalizedPeriod + 1 < capellaCurrentPeriod)        
            {
                var startPeriod = capellaFinalizedPeriod + 1;            
                var count = capellaCurrentPeriod - capellaFinalizedPeriod - 1;

                _syncProtocol.SetLightClientUpdatesByRangeRequest(startPeriod, count);

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