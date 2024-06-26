using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Sync;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Multiformats.Address;
using NUnit.Framework;
using SszSharp;

namespace Lantern.Beacon.Tests;

public class CustomDiscoveryProtocolTests
{
    private Mock<IDiscv5Protocol> _mockDiscv5Protocol;
    private Mock<IIdentityManager> _mockIdentityManager;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private CustomDiscoveryProtocol _customDiscoveryProtocol;
    private SyncProtocol _syncProtocol;

    [SetUp]
    public void Setup()
    {
        _mockDiscv5Protocol = new Mock<IDiscv5Protocol>();
        _mockIdentityManager = new Mock<IIdentityManager>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger<CustomDiscoveryProtocol>>());
        var syncProtocolOptions = new SyncProtocolOptions
        {
            Preset = SizePreset.MainnetPreset,
            GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
            GenesisTime = 1606824023,
        };
        _syncProtocol = new SyncProtocol(syncProtocolOptions, _mockLoggerFactory.Object);
        _syncProtocol.Init();
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
    }

    [Test]
    public async Task InitAsync_ShouldReturnTrue_WhenDiscv5ProtocolInitializesSuccessfully()
    {
        var mockEnr = new Mock<IEnr>();
        mockEnr.Setup(e => e.GetEntry(EnrEntryKey.Udp, It.IsAny<EntryUdp>())).Returns(new EntryUdp(30303));
        
        _mockIdentityManager.Setup(m => m.Record).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.SelfEnr).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.InitAsync()).ReturnsAsync(true);

        var result = await _customDiscoveryProtocol.InitAsync();

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

        var result = await _customDiscoveryProtocol.InitAsync();

        Assert.That(result, Is.False);
        
        _mockIdentityManager.Verify(m => m.Record.UpdateEntry(It.IsAny<EntryTcp>()), Times.Never);
    }
    
    [Test]
    public async Task StopAsync_ShouldInvokeStopOnDiscv5Protocol()
    {
        await _customDiscoveryProtocol.StopAsync();
        _mockDiscv5Protocol.Verify(p => p.StopAsync(), Times.Once);
    }

    [Test]
    public async Task DiscoverAsync_ShouldReturnPeers_WhenPeersAreAvailable()
    {
        var nodeId = new byte[32];
        _mockDiscv5Protocol.Setup(p => p.DiscoverAsync(nodeId)).ReturnsAsync(new List<IEnr> { Mock.Of<IEnr>() });

        var result = _customDiscoveryProtocol.DiscoverAsync(Multiaddress.Decode("/ip4/127.0.0.1/tcp/4001"));
        
        Assert.That(result, Is.Not.Null);
    }
}