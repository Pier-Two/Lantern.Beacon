using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class CoreTests
{
    [Test]
    public void Test()
    {
        var denebLightClientStore = DenebLightClientStore.CreateDefault();
        var result = denebLightClientStore.Equals(DenebLightClientStore.CreateDefault());
        Console.WriteLine(result);
    }
}