using System.Reflection;
using Lantern.Beacon.Networking.Libp2pProtocols.Identify;
using Moq;
using Nethermind.Libp2p.Core;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class BeaconClientPeerFactoryTests
{
    private BeaconClientPeerFactory _beaconClientPeerFactory;
    
    [SetUp]
    public void Setup()
    {
        _beaconClientPeerFactory = new BeaconClientPeerFactory(null);
    }
        
    [Test]
    public async Task ConnectedTo_ShouldDialPeerIdentifyProtocol()
    {
        var mockRemotePeer = new Mock<IRemotePeer>();
        var method = typeof(BeaconClientPeerFactory).GetMethod("ConnectedTo", BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            Assert.Fail("The method 'ConnectedTo' was not found.");
        }
        else
        {
            var task = (Task)method.Invoke(_beaconClientPeerFactory, [mockRemotePeer.Object, true]);
            await task;
        }
            
        mockRemotePeer.Verify(x => x.DialAsync<PeerIdentifyProtocol>(new CancellationToken()), Times.Once());
    }
    
    [Test]
    public void Create_ShouldReturnLocalPeer()
    {
        var localPeer = _beaconClientPeerFactory.Create();
        
        Assert.That(localPeer, Is.Not.Null);
    }
}