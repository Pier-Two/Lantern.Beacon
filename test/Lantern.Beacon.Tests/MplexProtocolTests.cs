using System.Reflection;
using System.Runtime.CompilerServices;
using Lantern.Beacon.Networking.Libp2pProtocols.Mplex;
using Microsoft.Extensions.Logging;
using Moq;
using Nethermind.Libp2p.Core;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class MplexProtocolTests
{
    private Mock<IChannel> _mockDownChannel;
    private Mock<IChannelFactory> _mockChannelFactory;
    private Mock<IPeerContext> _mockPeerContext;
    private Mock<ILoggerFactory> _mockLoggerFactory;
    private Mock<ILogger<MplexProtocol>> _mockLogger;
    private MplexProtocol _mplexProtocol;
    
    [SetUp]
    public void Setup()
    {
        _mockDownChannel = new Mock<IChannel>();
        _mockChannelFactory = new Mock<IChannelFactory>();
        _mockPeerContext = new Mock<IPeerContext>();
        _mockLogger = new Mock<ILogger<MplexProtocol>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object); 
    }

    [Test]
    public void ConnectAsync_ShouldThrowWhenContextIsNull()
    {
        _mplexProtocol = new MplexProtocol(null, _mockLoggerFactory.Object);
        
        var connectAsyncMethod = _mplexProtocol.GetType()
            .GetMethod("ConnectAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        
        Assert.ThrowsAsync<ArgumentNullException>(() => (Task)connectAsyncMethod.Invoke(_mplexProtocol, [_mockDownChannel.Object, _mockChannelFactory.Object, null, true]));
    }
    
    [Test]
    public void ConnectAsync_ShouldThrowWhenChannelFactoryIsNull()
    {
        _mplexProtocol = new MplexProtocol(null, _mockLoggerFactory.Object);
        
        var connectAsyncMethod = _mplexProtocol.GetType()
            .GetMethod("ConnectAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        
        Assert.ThrowsAsync<ArgumentNullException>(() => (Task)connectAsyncMethod.Invoke(_mplexProtocol, [_mockDownChannel.Object, null, _mockPeerContext.Object, true]));
    }
    
    [Test]
    public void ConnectAsync_ShouldNotThrowWhenAllArgumentsProvided()
    {
        _mplexProtocol = new MplexProtocol(null, _mockLoggerFactory.Object);
        _mockDownChannel.Setup(x => x.GetAwaiter()).Returns(new TaskAwaiter());
        
        var connectAsyncMethod = _mplexProtocol.GetType()
            .GetMethod("ConnectAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        
        connectAsyncMethod.Invoke(_mplexProtocol, [_mockDownChannel.Object, _mockChannelFactory.Object, _mockPeerContext.Object, true]);
        
        _mockDownChannel.Verify(x => x.GetAwaiter(), Times.Once);
        _mockPeerContext.Verify(x => x.Connected(It.IsAny<IPeer>()), Times.Once);
    }
    
   
    
    
    
    
    
}