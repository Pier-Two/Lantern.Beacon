using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols.Pubsub;

namespace Lantern.Beacon.Tests;

[TestFixture]
 public class BeaconClientTests
{
    private Mock<IPeerState> _mockPeerState;
    private Mock<IPeerFactoryBuilder> _mockPeerFactoryBuilder;
    private Mock<IBeaconClientManager> _mockBeaconClientManager;
    private Mock<ILiteDbService> _mockLiteDbService;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<ILogger<BeaconClient>> _mockLogger;
    private Mock<ISyncProtocol> _mockSyncProtocol;
    private Mock<IGossipSubManager> _mockGossipSubManager;
    private Mock<IServiceProvider> _mockServiceProvider;
    private BeaconClient _beaconClient;
    
    [SetUp]
    public void Setup()
    {
        _mockPeerState = new Mock<IPeerState>();
        _mockPeerFactoryBuilder = new Mock<IPeerFactoryBuilder>();
        _mockBeaconClientManager = new Mock<IBeaconClientManager>();
        _mockLiteDbService = new Mock<ILiteDbService>();
        _mockLogger = new Mock<ILogger<BeaconClient>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockSyncProtocol = new Mock<ISyncProtocol>();
        _mockGossipSubManager = new Mock<IGossipSubManager>();
        _mockServiceProvider = new Mock<IServiceProvider>();
    }

    [Test]
    public async Task InitAsync_ShouldInitializeCorrectly()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _mockGossipSubManager.Setup(x => x.LightClientFinalityUpdate).Returns(new Mock<ITopic>().Object);
        _mockGossipSubManager.Setup(x => x.LightClientOptimisticUpdate).Returns(new Mock<ITopic>().Object);
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        await _beaconClient.InitAsync();
        
        _mockLiteDbService.Verify(x => x.Init(), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<AltairLightClientStore>(nameof(AltairLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<CapellaLightClientStore>(nameof(CapellaLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientStore>(nameof(DenebLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientFinalityUpdate>(nameof(DenebLightClientFinalityUpdate)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientOptimisticUpdate>(nameof(DenebLightClientOptimisticUpdate)), Times.Once);
        _mockSyncProtocol.Verify(x => x.Init(It.IsAny<AltairLightClientStore>(), It.IsAny<CapellaLightClientStore>(), It.IsAny<DenebLightClientStore>(), It.IsAny<DenebLightClientFinalityUpdate>(), It.IsAny<DenebLightClientOptimisticUpdate>()), Times.Once);
        _mockPeerState.Verify(x => x.Init(It.IsAny<IEnumerable<IProtocol>>()), Times.Once);
        _mockGossipSubManager.Verify(x => x.Init(), Times.Once);
        _mockBeaconClientManager.Verify(x => x.InitAsync(), Times.Once);
    }
    
    [Test]
    public async Task InitAsync_ShouldThrowIfExceptionOccurs()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _mockGossipSubManager.Setup(x => x.LightClientFinalityUpdate).Returns(new Mock<ITopic>().Object);
        _mockGossipSubManager.Setup(x => x.LightClientOptimisticUpdate).Returns(new Mock<ITopic>().Object);
        _mockGossipSubManager.Setup(x => x.Init()).Throws(new Exception());
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        Assert.ThrowsAsync<Exception>(async () => await _beaconClient.InitAsync());
        
        _mockLiteDbService.Verify(x => x.Init(), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<AltairLightClientStore>(nameof(AltairLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<CapellaLightClientStore>(nameof(CapellaLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientStore>(nameof(DenebLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientFinalityUpdate>(nameof(DenebLightClientFinalityUpdate)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientOptimisticUpdate>(nameof(DenebLightClientOptimisticUpdate)), Times.Once);
        _mockSyncProtocol.Verify(x => x.Init(It.IsAny<AltairLightClientStore>(), It.IsAny<CapellaLightClientStore>(), It.IsAny<DenebLightClientStore>(), It.IsAny<DenebLightClientFinalityUpdate>(), It.IsAny<DenebLightClientOptimisticUpdate>()), Times.Once);
        _mockPeerState.Verify(x => x.Init(It.IsAny<IEnumerable<IProtocol>>()), Times.Once);
        _mockGossipSubManager.Verify(x => x.Init(), Times.Once);
        _mockBeaconClientManager.Verify(x => x.InitAsync(), Times.Never);
    }
    
    [Test]
    public async Task InitAsync_ShouldNotInitializeIfTopicsAreNotSet()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        await _beaconClient.InitAsync();
        
        _mockLiteDbService.Verify(x => x.Init(), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<AltairLightClientStore>(nameof(AltairLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<CapellaLightClientStore>(nameof(CapellaLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientStore>(nameof(DenebLightClientStore)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientFinalityUpdate>(nameof(DenebLightClientFinalityUpdate)), Times.Once);
        _mockLiteDbService.Verify(x => x.Fetch<DenebLightClientOptimisticUpdate>(nameof(DenebLightClientOptimisticUpdate)), Times.Once);
        _mockSyncProtocol.Verify(x => x.Init(It.IsAny<AltairLightClientStore>(), It.IsAny<CapellaLightClientStore>(), It.IsAny<DenebLightClientStore>(), It.IsAny<DenebLightClientFinalityUpdate>(), It.IsAny<DenebLightClientOptimisticUpdate>()), Times.Once);
        _mockPeerState.Verify(x => x.Init(It.IsAny<IEnumerable<IProtocol>>()), Times.Once);
        _mockGossipSubManager.Verify(x => x.Init(), Times.Once);
        _mockBeaconClientManager.Verify(x => x.InitAsync(), Times.Never);
    }
    
    [Test]
    public async Task StartAsync_ShouldStartProperly()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        await _beaconClient.StartAsync();

        _mockGossipSubManager.Verify(x => x.Start(It.IsAny<CancellationToken>()), Times.Once);
        _mockBeaconClientManager.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Test]
    public void StartAsync_ShouldThrowIfExceptionOccurs()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _mockGossipSubManager.Setup(x => x.Start(new CancellationToken())).Throws(new Exception());

        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        Assert.ThrowsAsync<Exception>(async () => await _beaconClient.StartAsync());

        _mockGossipSubManager.Verify(x => x.Start(It.IsAny<CancellationToken>()), Times.Once);
        _mockBeaconClientManager.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Test]
    public async Task StopAsync_ShouldStopProperly()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        _beaconClient.StartAsync();
        await _beaconClient.StopAsync();
        
        _mockGossipSubManager.Verify(x => x.StopAsync(), Times.Once);
        _mockBeaconClientManager.Verify(x => x.StopAsync(), Times.Once);
        _mockLiteDbService.Verify(x => x.Dispose(), Times.Once);
        Assert.That(_beaconClient.CancellationTokenSource, Is.Null);
    }
    
    [Test]
    public async Task StopAsync_ShouldReturnIfCancellationTokenIsNull()
    {
        _mockBeaconClientManager.Setup(bc => bc.InitAsync());
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockPeerState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
        
        await _beaconClient.StopAsync();
        
        Assert.That(_beaconClient.CancellationTokenSource, Is.Null);
        _mockGossipSubManager.Verify(x => x.StopAsync(), Times.Never);
        _mockBeaconClientManager.Verify(x => x.StopAsync(), Times.Never);
        _mockLiteDbService.Verify(x => x.Dispose(), Times.Never);
    }
    
    
    
} 