using Lantern.Beacon.Networking.Encoding;
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
        var response = Convert.FromHexString("08FF060000734E61507059010C000002CD16568100000000000000");
        var decodedResponse = ReqRespHelpers.DecodeRequest(response);
        var goodbye = Goodbye.Deserialize(decodedResponse);
        Console.WriteLine(goodbye.Reason);
    }
}