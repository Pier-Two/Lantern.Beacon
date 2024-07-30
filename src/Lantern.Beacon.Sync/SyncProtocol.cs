using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Sync;

public class SyncProtocol(SyncProtocolOptions options, ILoggerFactory loggerFactory) : ISyncProtocol
{
    public AltairLightClientStore AltairLightClientStore { get; private set; } = AltairLightClientStore.CreateDefault();

    public CapellaLightClientStore CapellaLightClientStore { get; private set; } = CapellaLightClientStore.CreateDefault();

    public DenebLightClientStore DenebLightClientStore { get; private set; } = DenebLightClientStore.CreateDefault();
    
    public DenebLightClientOptimisticUpdate CurrentLightClientOptimisticUpdate { get; set; } = DenebLightClientOptimisticUpdate.CreateDefault();
    
    public DenebLightClientFinalityUpdate CurrentLightClientFinalityUpdate { get; set; } = DenebLightClientFinalityUpdate.CreateDefault();
    
    public ILogger<SyncProtocol>? Logger { get; } = loggerFactory.CreateLogger<SyncProtocol>();

    public ForkType ActiveFork { get; private set; } = ForkType.Phase0;

    public LightClientUpdatesByRangeRequest? LightClientUpdatesByRangeRequest { get; set; } 
    
    public bool IsInitialised { get; private set; }
    
    public SyncProtocolOptions Options => options;
    
    public void Init(AltairLightClientStore? altairStore, 
        CapellaLightClientStore? capellaStore, 
        DenebLightClientStore? denebStore,
        DenebLightClientFinalityUpdate? finalityUpdate,
        DenebLightClientOptimisticUpdate? optimisticUpdate)
    {
        if (options.Network.Equals(NetworkType.Mainnet)) 
        { 
            Config.Config.InitializeWithMainnet(); 
            Phase0Preset.InitializeWithMainnet(); 
            AltairPreset.InitializeWithMainnet(); 
        } 
        else if (options.Network.Equals(NetworkType.Holesky)) 
        { 
            Config.Config.InitializeWithHolesky(); 
            Phase0Preset.InitializeWithHolesky(); 
            AltairPreset.InitializeWithHolesky(); 
        } 
        else if (options.Network.Equals(NetworkType.Custom))
        {
            if(options is { ConfigSettings: not null, PresetSettings: not null })
            {
                Config.Config.InitializeWithCustom(options.ConfigSettings);
                Phase0Preset.InitializeWithCustom(options.PresetSettings);
                AltairPreset.InitializeWithCustom(options.PresetSettings);
            }
            else
            {
                throw new Exception("Custom config and preset settings not provided");
            }
        }
        else 
        { 
            throw new Exception("Invalid preset type"); 
        } 
        
        if(altairStore != null)
        {
            AltairLightClientStore = altairStore;
            IsInitialised = true;
            SetActiveFork(ForkType.Altair);
            Logger?.LogInformation("Light client store initialised");
        }
        else if(capellaStore != null)
        {
            CapellaLightClientStore = capellaStore;
            IsInitialised = true;
            SetActiveFork(ForkType.Capella);
            Logger?.LogInformation("Light client store initialised");
        }
        else if(denebStore != null)
        {
            DenebLightClientStore = denebStore;
            IsInitialised = true;
            SetActiveFork(ForkType.Deneb);
            Logger?.LogInformation("Light client store initialised");
        }
        
        if(finalityUpdate != null)
        {
            CurrentLightClientFinalityUpdate = finalityUpdate;
        }
        
        if(optimisticUpdate != null)
        {
            CurrentLightClientOptimisticUpdate = optimisticUpdate;
        }
    }
    
    public bool InitialiseStoreFromAltairBootstrap(byte[] trustedBlockRoot, AltairLightClientBootstrap bootstrap)
    {
        if (!AltairHelpers.IsValidLightClientHeader(bootstrap.Header)) 
        { 
            Logger?.LogError("Invalid light client header in bootstrap");
            return false;
        } 
        
        if (!trustedBlockRoot.SequenceEqual(bootstrap.Header.Beacon.GetHashTreeRoot(options.Preset))) 
        { 
            Logger?.LogError("Invalid trusted block root in bootstrap");
            return false;
        } 
        
        var leaf = bootstrap.CurrentSyncCommittee.GetHashTreeRoot(options.Preset); 
        var branch = bootstrap.CurrentSyncCommitteeBranch; 
        var depth = Constants.CurrentSyncCommitteeBranchDepth; 
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.CurrentSyncCommitteeGIndex); 
        var root = bootstrap.Header.Beacon.StateRoot; 
        
        if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root)) 
        { 
            Logger?.LogError("Invalid sync committee branch in bootstrap");
            return false;
        } 
        
        AltairLightClientStore = AltairLightClientStore.CreateFrom( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        );
        
        IsInitialised = true;
        
        return true;
    } 

    public bool InitialiseStoreFromCapellaBootstrap(byte[] trustedBlockRoot, CapellaLightClientBootstrap bootstrap) 
    { 
        if (!CapellaHelpers.IsValidLightClientHeader(bootstrap.Header, options.Preset)) 
        { 
            Logger?.LogError("Invalid light client header in bootstrap");
            return false;
        } 
        
        if (!trustedBlockRoot.SequenceEqual(bootstrap.Header.Beacon.GetHashTreeRoot(options.Preset))) 
        { 
            Logger?.LogError("Invalid trusted block root in bootstrap");
            return false;
        } 
        
        var leaf = bootstrap.CurrentSyncCommittee.GetHashTreeRoot(options.Preset); 
        var branch = bootstrap.CurrentSyncCommitteeBranch; 
        var depth = Constants.CurrentSyncCommitteeBranchDepth; 
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.CurrentSyncCommitteeGIndex); 
        var root = bootstrap.Header.Beacon.StateRoot; 
        
        if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root)) 
        { 
            Logger?.LogError("Invalid sync committee branch in bootstrap");
            return false;
        } 
        
        CapellaLightClientStore = CapellaLightClientStore.CreateFrom( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
        IsInitialised = true;
        
        return true;
    } 

    public bool InitialiseStoreFromDenebBootstrap(byte[] trustedBlockRoot, DenebLightClientBootstrap bootstrap) 
    { 
        if (!DenebHelpers.IsValidLightClientHeader(bootstrap.Header, options.Preset)) 
        { 
            Logger?.LogError("Invalid light client header in bootstrap");
            return false;
        } 
        
        if (!trustedBlockRoot.SequenceEqual(bootstrap.Header.Beacon.GetHashTreeRoot(options.Preset))) 
        { 
            Logger?.LogError("Invalid trusted block root in bootstrap");
            return false;
        } 
        
        var leaf = bootstrap.CurrentSyncCommittee.GetHashTreeRoot(options.Preset); 
        var branch = bootstrap.CurrentSyncCommitteeBranch; 
        var depth = Constants.CurrentSyncCommitteeBranchDepth; 
        var index = (int)AltairHelpers.GetSubtreeIndex(Constants.CurrentSyncCommitteeGIndex); 
        var root = bootstrap.Header.Beacon.StateRoot; 
        
        if (!Phase0Helpers.IsValidMerkleBranch(leaf, branch, depth, index, root)) 
        { 
            Logger?.LogError("Invalid sync committee branch in bootstrap");
            return false;
        } 
        
        DenebLightClientStore = DenebLightClientStore.CreateFrom( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
        IsInitialised = true;
        
        return true;
    }
    
    public void SetActiveFork(ForkType forkType) 
    {
        ActiveFork = forkType;
    }
}