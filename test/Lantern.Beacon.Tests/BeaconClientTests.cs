using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;
using Nethermind.Libp2p.Core;


namespace Lantern.Beacon.Tests;

[TestFixture]
 public class BeaconClientTests
{
    private Mock<IPeerState> _mockNetworkState;
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
        _mockNetworkState = new Mock<IPeerState>();
        _mockPeerFactoryBuilder = new Mock<IPeerFactoryBuilder>();
        _mockBeaconClientManager = new Mock<IBeaconClientManager>();
        _mockLiteDbService = new Mock<ILiteDbService>();
        _mockLogger = new Mock<ILogger<BeaconClient>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockSyncProtocol = new Mock<ISyncProtocol>();
        _mockGossipSubManager = new Mock<IGossipSubManager>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        _mockBeaconClientManager.Setup(bc => bc.InitAsync(new CancellationToken()));
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);

        if (_mockBeaconClientManager == null || _mockServiceProvider == null)
        {
            throw new InvalidOperationException("A required mock is null.");
        }
        
        _beaconClient = new BeaconClient(_mockSyncProtocol.Object, _mockLiteDbService.Object, _mockPeerFactoryBuilder.Object, _mockNetworkState.Object, _mockBeaconClientManager.Object, _mockGossipSubManager.Object, _mockServiceProvider.Object);
    }

    [Test]
    public async Task StartAsync_ShouldStartPeerManager()
    {
        var cancellationToken = new CancellationToken();
        
        _beaconClient.StartAsync(cancellationToken);
        
        _mockBeaconClientManager.Verify(pm => pm.StartAsync(cancellationToken), Times.Once);
    }

    [Test]
    public async Task StopAsync_ShouldStopDiscoveryProtocol()
    {
        await _beaconClient.StopAsync();
        
        _mockBeaconClientManager.Verify(dp => dp.StopAsync(), Times.Once);
    }
} 