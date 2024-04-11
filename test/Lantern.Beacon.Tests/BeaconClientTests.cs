﻿using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Moq;


namespace Lantern.Beacon.Tests;

[TestFixture]
public class BeaconClientTests
{
    private Mock<IDiscoveryProtocol> _mockDiscoveryProtocol;
    private Mock<IPeerManager> _mockPeerManager;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<ILogger<BeaconClient>> _mockLogger;
    private Mock<IServiceProvider> _mockServiceProvider;
    private BeaconClient _beaconClient;
    
    [SetUp]
    public void Setup()
    {
        // Initialize all mocks first
        _mockDiscoveryProtocol = new Mock<IDiscoveryProtocol>();
        _mockPeerManager = new Mock<IPeerManager>();
        _mockLogger = new Mock<ILogger<BeaconClient>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        // Setup the LoggerFactory to return the Logger mock
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

        // Setup the ServiceProvider to return the LoggerFactory mock
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_mockLoggerFactory.Object);

        // Ensure all mocks are initialized before creating BeaconClient
        if (_mockDiscoveryProtocol == null || _mockPeerManager == null || _mockServiceProvider == null)
        {
            throw new InvalidOperationException("A required mock is null.");
        }

        // Create BeaconClient with initialized mocks
        _beaconClient = new BeaconClient(_mockDiscoveryProtocol.Object, _mockPeerManager.Object, _mockServiceProvider.Object);
    }

    [Test]
    public async Task InitAsync_ShouldInitializePeerManager()
    {
        // Arrange
        CancellationToken cancellationToken = new CancellationToken();

        // Act
        await _beaconClient.InitAsync(cancellationToken);

        // Assert
        _mockPeerManager.Verify(pm => pm.InitAsync(cancellationToken), Times.Once);
    }

    [Test]
    public async Task StartAsync_ShouldStartPeerManager()
    {
        // Arrange
        CancellationToken cancellationToken = new CancellationToken();

        // Act
        await _beaconClient.StartAsync(cancellationToken);

        // Assert
        _mockPeerManager.Verify(pm => pm.StartAsync(cancellationToken), Times.Once);
    }

    [Test]
    public async Task StopAsync_ShouldStopDiscoveryProtocol()
    {
        // Act
        await _beaconClient.StopAsync();

        // Assert
        _mockDiscoveryProtocol.Verify(dp => dp.StopAsync(), Times.Once);
    }
    
    [Test]
    public void InitAsync_ShouldLogErrorOnException()
    {
        // Arrange
        var exception = new Exception("Initialization failed");
        _mockPeerManager.Setup(pm => pm.InitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _beaconClient.InitAsync());

        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Failed to start peer manager", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}