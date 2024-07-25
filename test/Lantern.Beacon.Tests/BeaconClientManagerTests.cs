using System.Net;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Sync;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;
using Moq;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Discovery;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

public class BeaconClientManagerTests
{
    private Mock<ILocalPeer> _mockLocalPeer;
    private Mock<IEnr> _mockEnr;
    private Mock<ManualDiscoveryProtocol> _manualDiscoveryProtocolMock;
    private Mock<ICustomDiscoveryProtocol> _mockCustomDiscoveryProtocol;
    private Mock<ISyncProtocol> _mockSyncProtocol;
    private Mock<IPeerState> _mockPeerState;
    private Mock<IIdentityManager> _mockIdentityManager;
    private Mock<IPeerFactory> _mockPeerFactory;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<ILogger<BeaconClientManager>> _mockLogger;
    private BeaconClientManager _beaconClientManager;
    
    [SetUp]
    public void Setup()
    {
        _mockLocalPeer = new Mock<ILocalPeer>();
        _mockEnr = new Mock<IEnr>();
        _manualDiscoveryProtocolMock = new Mock<ManualDiscoveryProtocol>();
        _mockCustomDiscoveryProtocol = new Mock<ICustomDiscoveryProtocol>();
        _mockIdentityManager = new Mock<IIdentityManager>();
        _mockSyncProtocol = new Mock<ISyncProtocol>();
        _mockPeerState = new Mock<IPeerState>();
        _mockPeerFactory = new Mock<IPeerFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<BeaconClientManager>>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        _beaconClientManager = new BeaconClientManager(new BeaconClientOptions(), _manualDiscoveryProtocolMock.Object, _mockCustomDiscoveryProtocol.Object, _mockPeerState.Object, _mockSyncProtocol.Object,_mockPeerFactory.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
    }

    [Test]
    public async Task InitAsync_SuccessfulInitialization_LogsInformation()
    {
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);
        
        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);
        
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryIp>())).Returns(new EntryIp(IPAddress.Parse("192.168.1.1")));
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        _mockEnr.Setup(x => x.GetEntry(It.IsAny<string>(), It.IsAny<EntryIp>())).Returns(new EntryIp(IPAddress.Parse("192.168.1.1")));
        _mockEnr.Setup(x => x.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        
        await _beaconClientManager.InitAsync();
        
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Beacon client manager started with")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}