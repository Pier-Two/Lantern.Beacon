using System.Collections.Concurrent;
using Google.Protobuf.Collections;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Altair;
using Lantern.Beacon.Sync.Types.Capella;
using Lantern.Beacon.Sync.Types.Deneb;
using Lantern.Beacon.Sync.Types.Phase0;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using SszSharp;

namespace Lantern.Beacon.Sync;

public class SyncProtocol(SyncProtocolOptions options, ILoggerFactory loggerFactory) : ISyncProtocol
{
    public ILogger<SyncProtocol>? Logger { get; } = loggerFactory.CreateLogger<SyncProtocol>(); 

    public AltairLightClientStore AltairLightClientStore { get; private set; } = AltairLightClientStore.CreateDefault();

    public CapellaLightClientStore CapellaLightClientStore { get; private set; } = CapellaLightClientStore.CreateDefault();

    public DenebLightClientStore DenebLightClientStore { get; private set; } = DenebLightClientStore.CreateDefault();
    
    public DenebLightClientOptimisticUpdate? DenebLightClientOptimisticUpdate { get; private set; } = DenebLightClientOptimisticUpdate.CreateDefault();
    
    public DenebLightClientFinalityUpdate? DenebLightClientFinalityUpdate { get; private set; } = DenebLightClientFinalityUpdate.CreateDefault();

    public SyncProtocolOptions Options => options;
    
    public MetaData MetaData { get; private set; }
    
    public ForkType ActiveFork { get; private set; } = ForkType.Phase0;
    
    public bool IsInitialized { get; private set; }
    
    public int PeerCount { get; set; }
    
    public IEnumerable<IProtocol> AppLayerProtocols { get; set; }
    
    public ConcurrentDictionary<PeerId, RepeatedField<string>> PeerProtocols { get; } = new();

    public LightClientUpdatesByRangeRequest? LightClientUpdatesByRangeRequest { get; private set; } =
        LightClientUpdatesByRangeRequest.CreateFrom(0, 0);

    // SyncProtocol implementation future change 
    // Create a type for each object that is compatible with the latest hardfork 
    // Create separate SSZ types for each hardfork 
    // Get rid of separate stores, processors, and helpers for each hardfork 

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
        
        MetaData = MetaData.CreateDefault(); 
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
        
        AltairLightClientStore = new AltairLightClientStore( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
        IsInitialized = true;
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
        
        CapellaLightClientStore = new CapellaLightClientStore( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
        IsInitialized = true;
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
        
        DenebLightClientStore = new DenebLightClientStore( 
            bootstrap.Header, 
            bootstrap.CurrentSyncCommittee, 
            AltairSyncCommittee.CreateDefault(), 
            null, 
            bootstrap.Header, 
            0, 
            0
        ); 
        
        IsInitialized = true;
    }
    
    public void SetActiveFork(ForkType forkType) 
    { 
        if(ActiveFork != ForkType.Phase0) 
        { 
            throw new Exception("Fork already set"); 
        }
        
        ActiveFork = forkType;
    }
    
    public void SetLightClientUpdatesByRangeRequest(ulong startPeriod, ulong count) 
    {
        LightClientUpdatesByRangeRequest = LightClientUpdatesByRangeRequest.CreateFrom(startPeriod, count);
    }
    
    public void SetDenebLightClientOptimisticUpdate(DenebLightClientOptimisticUpdate optimisticUpdate) 
    { 
        DenebLightClientOptimisticUpdate = optimisticUpdate; 
    }
    
    public void SetDenebLightClientFinalityUpdate(DenebLightClientFinalityUpdate finalityUpdate) 
    { 
        DenebLightClientFinalityUpdate = finalityUpdate; 
    }
}