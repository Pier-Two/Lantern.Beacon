using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Presets;
using Microsoft.Extensions.Logging;
using Moq;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols.Pubsub;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

public class GossipSubManagerTests
{
    private SyncProtocolOptions _syncProtocolOptions;
    private PubsubRouter _router;
    private ManualDiscoveryProtocol _discoveryProtocol;
    private Mock<IBeaconClientManager> _beaconClientManager;
    private Mock<ISyncProtocol> _syncProtocol;
    private Mock<ILiteDbService> _liteDbService;
    private Mock<ILogger<GossipSubManager>> _mockLogger;
    private Mock<ILoggerFactory> _loggerFactory;
    private GossipSubManager? _gossipSubManager;

    [SetUp]
    public void Setup()
    {
        _router = new PubsubRouter();
        _discoveryProtocol = new ManualDiscoveryProtocol();
        _beaconClientManager = new Mock<IBeaconClientManager>();
        _syncProtocol = new Mock<ISyncProtocol>();
        _liteDbService = new Mock<ILiteDbService>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<GossipSubManager>>();
        _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object); 
    }
    
    [Test]
    public void Init_ShouldInitializeCorrectly()
    {
        _syncProtocolOptions = new SyncProtocolOptions
        {
            GenesisTime = 3999999999,
            GenesisValidatorsRoot = Convert.FromHexString("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"),
        };
        Config.InitializeWithMainnet();
        Phase0Preset.InitializeWithMainnet();
        AltairPreset.InitializeWithMainnet();
        
        _gossipSubManager = new GossipSubManager(_discoveryProtocol, _syncProtocolOptions, _router, _beaconClientManager.Object, _syncProtocol.Object, _liteDbService.Object, _loggerFactory.Object);
        
        Assert.DoesNotThrow(() => _gossipSubManager.Init());
        Assert.That(_gossipSubManager.LightClientFinalityUpdate, Is.Not.Null);
        Assert.That(_gossipSubManager.LightClientOptimisticUpdate, Is.Not.Null);
    }

    [Test]
    public void Start_ShouldThrowIfLocalPeerIsNull()
    {
        _syncProtocolOptions = new SyncProtocolOptions
        {
            GenesisTime = 3999999999,
            GenesisValidatorsRoot =
                Convert.FromHexString("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef")
        };
        Config.InitializeWithMainnet();
        Phase0Preset.InitializeWithMainnet();
        AltairPreset.InitializeWithMainnet();

        _beaconClientManager.Setup(x => x.LocalPeer).Returns(() => null);
        _gossipSubManager = new GossipSubManager(_discoveryProtocol, _syncProtocolOptions, _router,
            _beaconClientManager.Object, _syncProtocol.Object, _liteDbService.Object, _loggerFactory.Object);

        Assert.Throws<Exception>(() => _gossipSubManager.StartAsync());
    }
    
    [Test]
    public void Start_ShouldStartGossipSubProtocol()
    {
        _syncProtocolOptions = new SyncProtocolOptions
        {
            GenesisTime = 3999999999,
            GenesisValidatorsRoot =
                Convert.FromHexString("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef")
        };
        Config.InitializeWithMainnet();
        Phase0Preset.InitializeWithMainnet();
        AltairPreset.InitializeWithMainnet();

        _beaconClientManager.Setup(x => x.LocalPeer).Returns(new Mock<ILocalPeer>().Object);
        _gossipSubManager = new GossipSubManager(_discoveryProtocol, _syncProtocolOptions, _router,
            _beaconClientManager.Object, _syncProtocol.Object, _liteDbService.Object, _loggerFactory.Object);

        Assert.DoesNotThrow(() => _gossipSubManager.StartAsync());
    }
}