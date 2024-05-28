using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Deneb;
using Microsoft.Extensions.Logging;
using SszSharp;

namespace Lantern.Beacon.Sync;

public class SyncProtocol(SyncProtocolOptions options, ILogger<SyncProtocol> logger) : ISyncProtocol
{
    private CancellationTokenSource? _cancellationTokenSource;
    public AltairLightClientStore _altairLightClientStore;
    public CapellaLightClientStore _capellaLightClientStore;
    public DenebLightClientStore _denebLightClientStore;
    
    // SyncProtocol implementation future change
    // Create a type for each object that is compatible with the latest hardfork 
    // Create separate SSZ types for each hardfork
    // Get rid of separate stores, processors, and helpers for each hardfork

    public async Task InitAsync()
    {
        if (options.Preset.Equals(SizePreset.MainnetPreset))
        {
            Config.Config.InitializeWithMainnet();
            Phase0Preset.InitializeWithMainnet();
            AltairPreset.InitializeWithMainnet();
        }
        else if (options.Preset.Equals(SizePreset.MinimalPreset))
        {
            Config.Config.InitializeWithMinimal();
            Phase0Preset.InitializeWithMinimal();
            AltairPreset.InitializeWithMinimal();
        }
        else
        {
            throw new Exception("Invalid settings type");
        }
    }
    
    public async Task StartAsync(CancellationToken token)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var genesisTime = Config.Config.MinGenesisTime;
            var secondsPerSlot = Config.Config.SecondsPerSlot;
            var currentSlot = (currentTime - genesisTime) / secondsPerSlot;
            
            logger.LogInformation("Current slot: {CurrentSlot}", currentSlot);
            
            // Add a dependency injection for p2p data and process it here
            await Task.Delay(secondsPerSlot * 1000, _cancellationTokenSource.Token);
        }
    }
    
    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }
        
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
        
        // Add a dependency injection for p2p data and stop it here
    }
    
    public void InitialiseStoreFromAltairBootstrap(byte[] trustedBlockRoot, AltairLightClientBootstrap bootstrap)
    {
        if (!AltairHelpers.IsValidLightClientHeader(bootstrap.Header))
        {
            throw new Exception("Invalid light client header in bootstrap");
        }

        if (!trustedBlockRoot.SequenceEqual(bootstrap.Header.Beacon.GetHashTreeRoot(options.Preset)))
        {
            throw new Exception("Invalid trusted block root in bootstrap");
        }
        
        var leaf = bootstrap.CurrentSyncCommittee.GetHashTreeRoot(options.Preset);
        var branch = bootstrap.CurrentSyncCommitteeBranch;
        var depth = Constants.CurrentSyncCommitteeBranchDepth;
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.CurrentSyncCommitteeGIndex);
        var root = bootstrap.Header.Beacon.StateRoot;

        if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root))
        {
            throw new Exception("Invalid sync committee branch in bootstrap");
        }
        
        _altairLightClientStore = new AltairLightClientStore(
            bootstrap.Header,
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0,
            0);
    }
    
    public void InitialiseStoreFromCapellaBootstrap(byte[] trustedBlockRoot, CapellaLightClientBootstrap bootstrap)
    {
        if (!CapellaHelpers.IsValidLightClientHeader(bootstrap.Header, options.Preset))
        {
            throw new Exception("Invalid light client header in bootstrap");
        }
        
        if (!trustedBlockRoot.SequenceEqual(bootstrap.Header.Beacon.GetHashTreeRoot(options.Preset)))
        {
            throw new Exception("Invalid trusted block root in bootstrap");
        }
        
        var leaf = bootstrap.CurrentSyncCommittee.GetHashTreeRoot(options.Preset);
        var branch = bootstrap.CurrentSyncCommitteeBranch;
        var depth = Constants.CurrentSyncCommitteeBranchDepth;
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.CurrentSyncCommitteeGIndex);
        var root = bootstrap.Header.Beacon.StateRoot;

        if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root))
        {
            throw new Exception("Invalid sync committee branch in bootstrap");
        }
        
        _capellaLightClientStore = new CapellaLightClientStore(
            bootstrap.Header,
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0,
            0);
    }
    
    public void InitialiseStoreFromDenebBootstrap(byte[] trustedBlockRoot, DenebLightClientBootstrap bootstrap)
    {
        if (!DenebHelpers.IsValidLightClientHeader(bootstrap.Header, options.Preset))
        {
            throw new Exception("Invalid light client header in bootstrap");
        }
        
        if (!trustedBlockRoot.SequenceEqual(bootstrap.Header.Beacon.GetHashTreeRoot(options.Preset)))
        {
            throw new Exception("Invalid trusted block root in bootstrap");
        }
        
        var leaf = bootstrap.CurrentSyncCommittee.GetHashTreeRoot(options.Preset);
        var branch = bootstrap.CurrentSyncCommitteeBranch;
        var depth = Constants.CurrentSyncCommitteeBranchDepth;
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.CurrentSyncCommitteeGIndex);
        var root = bootstrap.Header.Beacon.StateRoot;

        if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root))
        {
            throw new Exception("Invalid sync committee branch in bootstrap");
        }
        
        _denebLightClientStore = new DenebLightClientStore(
            bootstrap.Header,
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0,
            0);
    }
}