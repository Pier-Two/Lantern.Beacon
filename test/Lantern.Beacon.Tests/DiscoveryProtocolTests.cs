using Lantern.Beacon.Networking.Discovery;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

public class DiscoveryProtocolTests
{
    private Mock<IDiscv5Protocol> _mockDiscv5Protocol;
    private Mock<IIdentityManager> _mockIdentityManager;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private DiscoveryProtocol _discoveryProtocol;

    [SetUp]
    public void Setup()
    {
        _mockDiscv5Protocol = new Mock<IDiscv5Protocol>();
        _mockIdentityManager = new Mock<IIdentityManager>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger<DiscoveryProtocol>>());

        _discoveryProtocol = new DiscoveryProtocol(new BeaconClientOptions() ,_mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
    }

    [Test]
    public async Task InitAsync_ShouldReturnTrue_WhenDiscv5ProtocolInitializesSuccessfully()
    {
        var mockEnr = new Mock<IEnr>();
        mockEnr.Setup(e => e.GetEntry(EnrEntryKey.Udp, It.IsAny<EntryUdp>())).Returns(new EntryUdp(30303));
        
        _mockIdentityManager.Setup(m => m.Record).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.SelfEnr).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.InitAsync()).ReturnsAsync(true);

        var result = await _discoveryProtocol.InitAsync();

        Assert.That(result, Is.True);
        
        _mockIdentityManager.Verify(m => m.Record.UpdateEntry(It.IsAny<EntryTcp>()), Times.Once);
    }
    
    [Test]
    public async Task InitAsync_ShouldReturnFalse_WhenDiscv5ProtocolFailsToInitializeSuccessfully()
    {
        var mockEnr = new Mock<IEnr>();
        mockEnr.Setup(e => e.GetEntry(EnrEntryKey.Udp, It.IsAny<EntryUdp>())).Returns(new EntryUdp(30303));
        
        _mockIdentityManager.Setup(m => m.Record).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.SelfEnr).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.InitAsync()).ReturnsAsync(false);

        var result = await _discoveryProtocol.InitAsync();

        Assert.That(result, Is.False);
        
        _mockIdentityManager.Verify(m => m.Record.UpdateEntry(It.IsAny<EntryTcp>()), Times.Never);
    }
    
    [Test]
    public async Task StopAsync_ShouldInvokeStopOnDiscv5Protocol()
    {
        await _discoveryProtocol.StopAsync();
        _mockDiscv5Protocol.Verify(p => p.StopAsync(), Times.Once);
    }

    [Test]
    public async Task DiscoverAsync_ShouldReturnPeers_WhenPeersAreAvailable()
    {
        var nodeId = new byte[32];
        _mockDiscv5Protocol.Setup(p => p.DiscoverAsync(nodeId)).ReturnsAsync(new List<IEnr> { Mock.Of<IEnr>() });

        var result = _discoveryProtocol.DiscoverAsync(nodeId);
        
        Assert.That(result, Is.Not.Null);
    }
}