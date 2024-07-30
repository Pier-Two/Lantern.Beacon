using System.Reflection;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

public class BeaconClientPeerFactoryBuilderTests
{
    private BeaconClientPeerFactoryBuilder _builder;

    [SetUp]
    public void Setup()
    {
        _builder = new BeaconClientPeerFactoryBuilder();
    }

    [Test]
    public void WithPlaintextEnforced_Should_SetEnforcePlaintext()
    {
        _builder.WithPlaintextEnforced();
        
        var enforcePlaintextField = typeof(BeaconClientPeerFactoryBuilder)
            .GetField("enforcePlaintext", BindingFlags.NonPublic | BindingFlags.Instance);
        var enforcePlaintextValue = (bool)enforcePlaintextField.GetValue(_builder);
        
        Assert.That(enforcePlaintextValue, Is.True);
    }
}