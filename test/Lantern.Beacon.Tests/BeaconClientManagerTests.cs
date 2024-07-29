using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Lantern.Beacon.Networking;
using Lantern.Beacon.Networking.Discovery;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Nethermind.Libp2p.Core;
using NUnit.Framework;
using SszSharp;

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
    public async Task InitAsync_ShouldInitializeCorrectly()
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
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Beacon client manager started with address")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task InitAsync_ShouldAddBootnodesToQueue_WhenBootnodesProvided()
    {
        var bootnodes = new[] { "/ip4/69.175.102.62/tcp/31018/p2p/16Uiu2HAm2FWXMoKEsshxjXNsXmFwxPAm4eaWmcffFTGgNs3gi4Ww", "/ip4/73.186.232.187/tcp/9105/p2p/16Uiu2HAm37UA7fk8r2AnYtGLbddwkS2WEeSPTsjNDGh3gDW7VUBQ" };
        var clientOptions = new BeaconClientOptions { Bootnodes = bootnodes };
        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);
        
        _beaconClientManager = new BeaconClientManager(clientOptions, _manualDiscoveryProtocolMock.Object, _mockCustomDiscoveryProtocol.Object, _mockPeerState.Object, _mockSyncProtocol.Object, _mockPeerFactory.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryIp>())).Returns(new EntryIp(IPAddress.Parse("192.168.1.1")));
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        
        await _beaconClientManager.InitAsync();
        
        // Use reflection to access the private _peersToDial field
        var fieldInfo = typeof(BeaconClientManager).GetField("_peersToDial", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var peersToDial = (ConcurrentQueue<Multiaddress>)fieldInfo.GetValue(_beaconClientManager)!;
        
        Assert.That(bootnodes.Length, Is.EqualTo(peersToDial.Count));

        for(var i = 0; i < bootnodes.Length; i++)
        {
            var expectedAddress = Multiaddress.Decode(bootnodes[i]);
            Assert.That(expectedAddress, Is.EqualTo(peersToDial.ToArray()[i]));
        }

        _mockLogger.Verify(log => log.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Added bootnodes for dialing")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task InitAsync_ShouldLogError_WhenCustomDiscoveryProtocolInitFails()
    {
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(false);
        
        await _beaconClientManager.InitAsync();
        
        _mockLogger.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start beacon client manager")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ), Times.Once);
    }
    
    [Test]
    public async Task InitAsync_ShouldLogError_OnException()
    {
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ThrowsAsync(new Exception("Test Exception"));
        
        await _beaconClientManager.InitAsync();

        _mockLogger.Verify(log => log.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start beacon client manager")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ), Times.Once);
    }
    
    [Test]
    public void StartAsync_ShouldThrowException_WhenLocalPeerIsNotInitialized()
    {
        _beaconClientManager = new BeaconClientManager(new BeaconClientOptions(), _manualDiscoveryProtocolMock.Object, _mockCustomDiscoveryProtocol.Object, _mockPeerState.Object, _mockSyncProtocol.Object, _mockPeerFactory.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
        Assert.ThrowsAsync<Exception>(async () => await _beaconClientManager.StartAsync(), "Local peer is not initialized. Cannot start peer manager");
    }

    [Test]
    public async Task StartAsync_ShouldStartWithSuccessfulInitialization()
    {
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);

        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        _mockPeerState.Setup(x => x.LivePeers).Returns(new ConcurrentDictionary<PeerId, IRemotePeer>());
        _mockSyncProtocol.Setup(x => x.Options).Returns(new SyncProtocolOptions());
        
        await _beaconClientManager.InitAsync();
        
        var startTask = _beaconClientManager.StartAsync();
        
        Assert.That(_beaconClientManager.LocalPeer, Is.Not.Null);
        await Task.WhenAny(startTask, Task.Delay(1000)); // Wait a bit to make sure it started
        Assert.That(startTask.IsCompleted, Is.False, "StartAsync should not complete immediately");
    }

    [Test]
    public async Task StopAsync_ShouldHandleCancellation()
    {
        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);

        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        _mockPeerState.Setup(x => x.LivePeers).Returns(new ConcurrentDictionary<PeerId, IRemotePeer>());
        _mockSyncProtocol.Setup(x => x.Options).Returns(new SyncProtocolOptions());
        
        await _beaconClientManager.InitAsync();
        
        var cts = new CancellationTokenSource();
        var startTask = _beaconClientManager.StartAsync(cts.Token);
        
        await Task.Delay(500, cts.Token);
        await _beaconClientManager.StopAsync();
        
        await cts.CancelAsync();
    
        Assert.That(startTask.IsCompleted, Is.True, "StartAsync should be canceled when the cancellation token is triggered");
    }
    
    [Test]
    public async Task DisplaySyncStatus_ShouldLogInformation()
    {
        var bootnodes = new[] { "/ip4/69.175.102.62/tcp/31018/p2p/16Uiu2HAm2FWXMoKEsshxjXNsXmFwxPAm4eaWmcffFTGgNs3gi4Ww", "/ip4/73.186.232.187/tcp/9105/p2p/16Uiu2HAm37UA7fk8r2AnYtGLbddwkS2WEeSPTsjNDGh3gDW7VUBQ" };
        var clientOptions = new BeaconClientOptions { Bootnodes = bootnodes };
        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);
        
        _beaconClientManager = new BeaconClientManager(clientOptions, _manualDiscoveryProtocolMock.Object, _mockCustomDiscoveryProtocol.Object, _mockPeerState.Object, _mockSyncProtocol.Object, _mockPeerFactory.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryIp>())).Returns(new EntryIp(IPAddress.Parse("192.168.1.1")));
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        
        await _beaconClientManager.InitAsync();
        
        var cts = new CancellationTokenSource();
        _beaconClientManager.StartAsync(cts.Token);
        
        await Task.Delay(1000, cts.Token);
        
        await _beaconClientManager.StopAsync();
        
        _mockLogger.Verify(log => log.Log(
                LogLevel.Information, 
                It.IsAny<EventId>(), 
                It.IsAny<It.IsAnyType>(), 
                null, 
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), 
            Times.AtLeastOnce);
    }
    
    [Test]
    public async Task ProcessPeerDiscoveryAsync_ShouldRunCorrectly()
    {
        var clientOptions = new BeaconClientOptions { EnableDiscovery = true, TargetPeerCount = 1 };
        var multiAddress = new Multiaddress().Add<IP4>("0.0.0.0").Add<TCP>(0);
        var syncOptions = new SyncProtocolOptions()
        {
            GenesisValidatorsRoot = new byte[32],
            GenesisTime = 1606824023,
            Preset = SizePreset.MainnetPreset,
        };
        var denebLightClientStore = DenebLightClientStore.CreateDefault();
        
        Phase0Preset.InitializeWithMainnet();
        AltairPreset.InitializeWithMainnet();
        Config.InitializeWithMainnet(); 
        
        _mockCustomDiscoveryProtocol.Setup(x => x.InitAsync()).ReturnsAsync(true);
        _mockLocalPeer.Setup(x => x.Address).Returns(multiAddress);
        _mockPeerFactory.Setup(x => x.Create(It.IsAny<Identity?>(), It.IsAny<Multiaddress?>())).Returns(_mockLocalPeer.Object);
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryIp>())).Returns(new EntryIp(IPAddress.Parse("192.168.1.1")));
        _mockIdentityManager.Setup(x => x.Record.GetEntry(It.IsAny<string>(), It.IsAny<EntryTcp>())).Returns(new EntryTcp(8080));
        _mockPeerState.Setup(x => x.LivePeers).Returns(new ConcurrentDictionary<PeerId, IRemotePeer>());
        _mockSyncProtocol.Setup(x => x.DenebLightClientStore).Returns(denebLightClientStore);
        _mockSyncProtocol.Setup(x => x.Options).Returns(syncOptions);
        _beaconClientManager = new BeaconClientManager(clientOptions, _manualDiscoveryProtocolMock.Object, _mockCustomDiscoveryProtocol.Object, _mockPeerState.Object, _mockSyncProtocol.Object, _mockPeerFactory.Object, _mockIdentityManager.Object, _mockLoggerFactory.Object);

        await _beaconClientManager.InitAsync();

        var cts = new CancellationTokenSource(2000); 
        await _beaconClientManager.StartAsync(cts.Token);
    
        _mockCustomDiscoveryProtocol.Verify(x => x.GetDiscoveredNodesAsync(It.IsAny<Multiaddress>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}