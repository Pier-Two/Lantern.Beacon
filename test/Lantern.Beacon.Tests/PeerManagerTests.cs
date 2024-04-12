using System.Net;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Microsoft.Extensions.Logging;
using Moq;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

public class PeerManagerTests
{
    private Mock<ILocalPeer> _mockLocalPeer;
    private Mock<IEnr> _mockEnr;
    private Mock<IDiscoveryProtocol> _mockDiscoveryProtocol;
    private Mock<IPeerFactory> _mockPeerFactory;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<ILogger<PeerManager>> _mockLogger;
    private PeerManager _peerManager;
    
    [SetUp]
    public void Setup()
    {
        _mockLocalPeer = new Mock<ILocalPeer>();
        _mockEnr = new Mock<IEnr>();
        _mockDiscoveryProtocol = new Mock<IDiscoveryProtocol>();
        _mockPeerFactory = new Mock<IPeerFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<PeerManager>>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _peerManager = new PeerManager(_mockDiscoveryProtocol.Object, _mockPeerFactory.Object, _mockLoggerFactory.Object);
    }

    [Test]
    public async Task InitAsync_SuccessfulInitialization_LogsInformation()
    {
        // Arrange
        _mockDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);
        
        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);
        
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        
        _mockEnr.Setup(x => x.GetEntry(It.IsAny<string>(), It.IsAny<EntryIp>())).Returns(new EntryIp(IPAddress.Parse("192.168.1.1")));
        _mockEnr.Setup(x => x.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        _mockDiscoveryProtocol.Setup(x => x.SelfEnr).Returns(_mockEnr.Object);
        
        // Act
        await _peerManager.InitAsync();

        // Assert
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Peer manager started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task InitAsync_FailedDiscoveryProtocol_LogsError()
    {
        // Arrange
        _mockDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(false);

        // Act
        await _peerManager.InitAsync();

        // Assert
        _mockLogger.Verify(log => log.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start peer manager")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}