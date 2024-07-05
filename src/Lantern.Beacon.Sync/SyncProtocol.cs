using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;
using SszSharp;

namespace Lantern.Beacon.Sync;

public class SyncProtocol(SyncProtocolOptions options, ILoggerFactory loggerFactory) : ISyncProtocol
{
    public ILogger<SyncProtocol>? Logger { get; } = loggerFactory.CreateLogger<SyncProtocol>(); 
    
    public AltairLightClientStore AltairLightClientStore { get; private set; } 

    public CapellaLightClientStore CapellaLightClientStore { get; private set; }

    public DenebLightClientStore DenebLightClientStore { get; private set; } 
    
    public LightClientUpdatesByRangeRequest? LightClientUpdatesByRangeRequest { get; set; } 
    
    public DenebLightClientOptimisticUpdate PreviousLightClientOptimisticUpdate { get; set; } 
    
    public DenebLightClientFinalityUpdate PreviousLightClientFinalityUpdate { get; set; } 

    public SyncProtocolOptions Options => options;
    
    public ForkType ActiveFork { get; private set; } = ForkType.Phase0;

    public void Init() 
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
            throw new Exception("Invalid preset type"); 
        } 
        
        AltairLightClientStore = AltairLightClientStore.CreateDefault();
        CapellaLightClientStore = CapellaLightClientStore.CreateDefault();
        DenebLightClientStore = DenebLightClientStore.CreateDefault();
        PreviousLightClientOptimisticUpdate = DenebLightClientOptimisticUpdate.CreateDefault();
        PreviousLightClientFinalityUpdate = DenebLightClientFinalityUpdate.CreateDefault();
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
        
        AltairLightClientStore = new AltairLightClientStore( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        );
        
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
        
        CapellaLightClientStore = new CapellaLightClientStore( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
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
        
        DenebLightClientStore = new DenebLightClientStore( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
        return true;
    }
    
    public void SetActiveFork(ForkType forkType) 
    { 
        if(ActiveFork != ForkType.Phase0) 
        { 
            throw new Exception("Fork already set"); 
        }
        
        ActiveFork = forkType;
    }
    
    public bool IsNotInitialised()
    {
        var result = ActiveFork switch
        {
            ForkType.Deneb => DenebLightClientStore.Equals(DenebLightClientStore.CreateDefault()),
            ForkType.Capella => CapellaLightClientStore.Equals(CapellaLightClientStore.CreateDefault()),
            ForkType.Bellatrix => AltairLightClientStore.Equals(AltairLightClientStore.CreateDefault()),
            ForkType.Altair => AltairLightClientStore.Equals(AltairLightClientStore.CreateDefault()),
            ForkType.Phase0 => false,
            _ => false
        };

        return result;
    }
}