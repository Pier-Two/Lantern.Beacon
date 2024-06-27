using Lantern.Beacon;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity.V4;
using Multiformats.Address.Protocols;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class BeaconChainUtility
{
    [Test]
    public void ConvertToMultiAddress_ShouldCorrectlyConvertToMultiAddress()
    {
        var enrRegistry = new EnrEntryRegistry();
        var enrString = "enr:-Mq4QLyFLj2R0kwCmxNgO02F2JqHOUAT9CnqK9qHBwJWPlvNR36e9YydkUzFM69E0dzX7hrpOUAJVKsBLb3PysSz-IiGAY7D6Sg4h2F0dG5ldHOIAAAAAAAAAAaEZXRoMpBqlaGpBAAAAP__________gmlkgnY0gmlwhCJkw5SJc2VjcDI1NmsxoQMc6eWKtIsR4Ref474zOEeRKEuHzxrK_jffZrkzzYSuUYhzeW5jbmV0cwCDdGNwgjLIg3VkcILLBIR1ZHA2gi7g";
        
        var enr = new EnrFactory(enrRegistry).CreateFromString(enrString, new IdentityVerifierV4());
        var multiAddress = BeaconClientUtility.ConvertToMultiAddress(enr);
        
        Assert.That(multiAddress, Is.Not.Null);
        Assert.That(enr.GetEntry<EntryIp>(EnrEntryKey.Ip).Value, Is.EqualTo(multiAddress!.Get<IP>().Value));
        Assert.That(enr.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value, Is.EqualTo(multiAddress.Get<TCP>().Value));
    }
}