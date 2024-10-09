using Lantern.Beacon.Networking.Libp2pProtocols.CustomPubsub;
using Lantern.Beacon.Sync;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.WireProtocol;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Moq;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Protocols;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class BeaconClientServiceBuilderTests
{
    private Mock<IServiceCollection> _mockServicesCollection;
    private Mock<IDiscv5ProtocolBuilder>? _mockDiscv5ProtocolBuilder;
    private BeaconClientServiceBuilder _builder;

    [SetUp]
    public void Setup()
    {
        _mockServicesCollection = new Mock<IServiceCollection>();
        _mockDiscv5ProtocolBuilder = new Mock<IDiscv5ProtocolBuilder>();
        _builder = new BeaconClientServiceBuilder(_mockServicesCollection.Object);
    }

    [Test]
    public void AddDiscoveryProtocol_WhenCalled_ShouldConfigureDiscv5ProtocolBuilder()
    {
        typeof(BeaconClientServiceBuilder)
            .GetField("_discv5ProtocolBuilder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_builder, _mockDiscv5ProtocolBuilder.Object);
        
        var configureCalled = false;

        _builder.AddDiscoveryProtocol(builder =>
        {
            configureCalled = true;
            Assert.That(builder, Is.EqualTo(_mockDiscv5ProtocolBuilder.Object));
        });

        Assert.That(configureCalled, Is.EqualTo(true));
    }
    
    [Test]
    public void AddLibp2pProtocol_WhenCalled_ShouldAddLibp2pProtocolToServices()
    {
        var mockPeerFactoryBuilder = new Mock<ILibp2pPeerFactoryBuilder>();

        _builder.AddLibp2pProtocol(FactorySetup);
        
        _mockServicesCollection.Verify(s =>
            s.Add(It.Is<ServiceDescriptor>(descriptor =>
                descriptor.ServiceType == typeof(CustomPubsubRouter) &&
                descriptor.Lifetime == ServiceLifetime.Scoped
            )), Times.Once);

        _mockServicesCollection.Verify(s =>
            s.Add(It.Is<ServiceDescriptor>(descriptor =>
                descriptor.ServiceType == typeof(MultiplexerSettings) &&
                descriptor.Lifetime == ServiceLifetime.Scoped
            )), Times.Once);

        _mockServicesCollection.Verify(s =>
            s.Add(It.Is<ServiceDescriptor>(descriptor =>
                descriptor.ServiceType == typeof(IdentifyProtocolSettings) &&
                descriptor.Lifetime == ServiceLifetime.Scoped &&
                descriptor.ImplementationFactory != null
            )), Times.Once);

        _mockServicesCollection.Verify(s =>
            s.Add(It.Is<ServiceDescriptor>(descriptor =>
                descriptor.ServiceType == typeof(IPeerFactory) &&
                descriptor.Lifetime == ServiceLifetime.Scoped
            )), Times.Once);
        return;

        IPeerFactoryBuilder FactorySetup(ILibp2pPeerFactoryBuilder _) => mockPeerFactoryBuilder.Object;
    }
    
    [Test]
    public void WithBeaconClientOptions_WhenCalled_ShouldConfigureBeaconClientOptions()
    {
        var beaconClientOptions = new BeaconClientOptions
        {
            TcpPort = 9000
        };

        _builder.WithBeaconClientOptions(beaconClientOptions);
        
        var actualOptions = typeof(BeaconClientServiceBuilder)
            .GetField("_beaconClientOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(_builder) as BeaconClientOptions;
        
        Assert.That(actualOptions, Is.Not.Null);
        Assert.That(actualOptions, Is.EqualTo(beaconClientOptions));
        Assert.That(actualOptions!.TcpPort, Is.EqualTo(9000));
    }
    
    [Test]
    public void WithLoggerFactory_WhenCalled_ShouldConfigureLoggerFactory()
    {
        var loggerFactory = new Mock<ILoggerFactory>().Object;

        _builder.WithLoggerFactory(loggerFactory);
        
        var actualLoggerFactory = typeof(BeaconClientServiceBuilder)
            .GetField("_loggerFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(_builder) as ILoggerFactory;
        
        Assert.That(actualLoggerFactory, Is.Not.Null);
        Assert.That(actualLoggerFactory, Is.EqualTo(loggerFactory));
    }
    
    [Test]
    public void Build_WhenCalled_ShouldRunProperly()
    {
        _mockDiscv5ProtocolBuilder.Setup(x => x.Build()).Returns(new Mock<IDiscv5Protocol>().Object);
        
        typeof(BeaconClientServiceBuilder)
            .GetField("_discv5ProtocolBuilder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_builder, _mockDiscv5ProtocolBuilder.Object);
     
        Assert.Throws<InvalidOperationException>(() => _builder.Build());
        
        _mockDiscv5ProtocolBuilder.Verify(x => x.Build(), Times.Once);
    }
}