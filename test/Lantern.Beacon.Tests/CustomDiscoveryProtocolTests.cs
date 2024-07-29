using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Sync;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity.V4;
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
    private SyncProtocolOptions _syncProtocolOptions;
    private Mock<IDiscv5Protocol> _mockDiscv5Protocol;
    private Mock<IIdentityManager> _mockIdentityManager;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private CustomDiscoveryProtocol _customDiscoveryProtocol;
    private SyncProtocol _syncProtocol;

    [SetUp]
    public void Setup()
    {
        _syncProtocolOptions = new SyncProtocolOptions
        {
            Preset = SizePreset.MainnetPreset,
            GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
            GenesisTime = 1606824023,
        };
        
        _mockDiscv5Protocol = new Mock<IDiscv5Protocol>();
        _mockIdentityManager = new Mock<IIdentityManager>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger<CustomDiscoveryProtocol>>());
        _syncProtocol = new SyncProtocol(_syncProtocolOptions, _mockLoggerFactory.Object);
        _syncProtocol.Init(null,null,null, null, null);
    }

    [Test]
    public async Task InitAsync_ShouldReturnTrue_WhenDiscv5ProtocolInitializesSuccessfully()
    {
        var mockEnr = new Mock<IEnr>();
        mockEnr.Setup(e => e.GetEntry(EnrEntryKey.Udp, It.IsAny<EntryUdp>())).Returns(new EntryUdp(30303));
        
        _mockIdentityManager.Setup(m => m.Record).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.SelfEnr).Returns(mockEnr.Object);
        _mockDiscv5Protocol.Setup(p => p.InitAsync()).ReturnsAsync(true);
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
        
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
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        var result = await _customDiscoveryProtocol.InitAsync();

        Assert.That(result, Is.False);
        
        _mockIdentityManager.Verify(m => m.Record.UpdateEntry(It.IsAny<EntryTcp>()), Times.Never);
    }
    
    [Test]
    public async Task StopAsync_ShouldInvokeStopOnDiscv5Protocol()
    {
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        await _customDiscoveryProtocol.StopAsync();
        _mockDiscv5Protocol.Verify(p => p.StopAsync(), Times.Once);
    }

    [Test]
    public void DiscoverAsync_ShouldReturnPeers_WhenPeersAreAvailable()
    {
        var nodeId = new byte[32];
        
        _mockDiscv5Protocol.Setup(p => p.DiscoverAsync(nodeId)).ReturnsAsync(new List<IEnr> { Mock.Of<IEnr>() });
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        var result = _customDiscoveryProtocol.DiscoverAsync(Multiaddress.Decode("/ip4/127.0.0.1/tcp/4001"));
        
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void GetDiscoveredNodesAsync_ShouldReturnIfNoActiveNodes()
    {
        var localPeerAddr = Multiaddress.Decode("/ip4/127.0.0.1/tcp/4001");
        
        _mockDiscv5Protocol.Setup(p => p.GetActiveNodes).Returns(new List<IEnr>());
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        var result = _customDiscoveryProtocol.GetDiscoveredNodesAsync(localPeerAddr);
        
        _mockDiscv5Protocol.Verify(x => x.SendFindNodeAsync(It.IsAny<IEnr>(), It.IsAny<byte[]>()), Times.Never);
    }
    
    [Test]
    public async Task GetDiscoveredNodesAsync_ShouldNotAddEnr()
    {
        var localPeerAddr = Multiaddress.Decode("/ip4/127.0.0.1/tcp/4001");
        var enrString = "enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8";
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(enrString, new IdentityVerifierV4());
        _mockDiscv5Protocol.Setup(p => p.GetActiveNodes).Returns(new List<IEnr>{ enr });

        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        var onAddPeerInvoked = false;
        
        _customDiscoveryProtocol.OnAddPeer = _ =>
        {
            onAddPeerInvoked = true;
            return true;
        };

        await _customDiscoveryProtocol.GetDiscoveredNodesAsync(localPeerAddr);
        
        _mockDiscv5Protocol.Verify(x => x.SendFindNodeAsync(It.IsAny<IEnr>(), It.IsAny<byte[]>()), Times.Once);
        
        Assert.That(onAddPeerInvoked, Is.False, "OnAddPeer was invoked.");
    }
    
    [Test]
    public async Task GetDiscoveredNodesAsync_ShouldReturnDiscoveredNodes()
    {
        var localPeerAddr = Multiaddress.Decode("/ip4/127.0.0.1/tcp/4001");
        var enrString = "enr:-Ly4QOS00hvPDddEcCpwA1cMykWNdJUK50AjbRgbLZ9FLPyBa78i0NwsQZLSV67elpJU71L1Pt9yqVmE1C6XeSI-LV8Bh2F0dG5ldHOIAAAAAAAAAACEZXRoMpDuKNezAAAAckYFAAAAAAAAgmlkgnY0gmlwhEDhTgGJc2VjcDI1NmsxoQIgMUMFvJGlr8dI1TEQy-K78u2TJE2rWvah9nGqLQCEGohzeW5jbmV0cwCDdGNwgiMog3VkcIIjKA";
        var enrEntryRegistry = new EnrEntryRegistry();
        var enr = new EnrFactory(enrEntryRegistry).CreateFromString(enrString, new IdentityVerifierV4());
        
        _mockDiscv5Protocol.Setup(p => p.GetActiveNodes).Returns(new List<IEnr>{ Mock.Of<IEnr>() });
        _mockDiscv5Protocol.Setup(p => p.SendFindNodeAsync(It.IsAny<IEnr>(), It.IsAny<byte[]>())).ReturnsAsync(new List<IEnr>{ enr });
        _customDiscoveryProtocol = new CustomDiscoveryProtocol(new BeaconClientOptions(), _syncProtocolOptions, _mockDiscv5Protocol.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        var onAddPeerInvoked = false;
        
        _customDiscoveryProtocol.OnAddPeer = _ =>
        {
            onAddPeerInvoked = true;
            return true;
        };

        await _customDiscoveryProtocol.GetDiscoveredNodesAsync(localPeerAddr);
        
        _mockDiscv5Protocol.Verify(x => x.SendFindNodeAsync(It.IsAny<IEnr>(), It.IsAny<byte[]>()), Times.AtLeastOnce);
        
        Assert.That(onAddPeerInvoked, Is.True, "OnAddPeer was not invoked.");
    }
    
}