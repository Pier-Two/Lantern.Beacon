using System.Net;
using Lantern.Beacon;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Config;
using Lantern.Beacon.Sync.Presets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Lantern.Discv5.Enr.Identity.V4;
using Lantern.Discv5.WireProtocol.Session;
using Multiformats.Address.Protocols;
using NUnit.Framework;
using SszSharp;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class BeaconChainUtilityTests
{
    [Test]
    public void ConvertToMultiAddress_ShouldCorrectlyConvertToMultiAddressForIpV4()
    {
        var enrRegistry = new EnrEntryRegistry();
        var enrString = "enr:-Mq4QLyFLj2R0kwCmxNgO02F2JqHOUAT9CnqK9qHBwJWPlvNR36e9YydkUzFM69E0dzX7hrpOUAJVKsBLb3PysSz-IiGAY7D6Sg4h2F0dG5ldHOIAAAAAAAAAAaEZXRoMpBqlaGpBAAAAP__________gmlkgnY0gmlwhCJkw5SJc2VjcDI1NmsxoQMc6eWKtIsR4Ref474zOEeRKEuHzxrK_jffZrkzzYSuUYhzeW5jbmV0cwCDdGNwgjLIg3VkcILLBIR1ZHA2gi7g";
        var enr = new EnrFactory(enrRegistry).CreateFromString(enrString, new IdentityVerifierV4());
        var multiAddress = BeaconClientUtility.ConvertToMultiAddress(enr);
        
        Assert.That(multiAddress, Is.Not.Null);
        Assert.That(multiAddress.ToString().Contains("ip4"), Is.True);
        Assert.That(enr.GetEntry<EntryIp>(EnrEntryKey.Ip).Value, Is.EqualTo(multiAddress!.Get<IP>().Value));
        Assert.That(enr.GetEntry<EntryTcp>(EnrEntryKey.Tcp).Value, Is.EqualTo(multiAddress.Get<TCP>().Value));
    }
    
    [Test]
    public void ConvertToMultiAddress_ShouldCorrectlyConvertToMultiAddressForIpV6()
    {
        var enrBuilder = new EnrBuilder();
        var sessionOptions = SessionOptions.Default;
        
        enrBuilder.WithIdentityScheme(sessionOptions.Verifier, sessionOptions.Signer);
        enrBuilder.WithEntry(EnrEntryKey.Id, new EntryId("v4"));
        enrBuilder.WithEntry(EnrEntryKey.Secp256K1, new EntrySecp256K1(sessionOptions.Signer.PublicKey));
        enrBuilder.WithEntry(EnrEntryKey.Ip6, new EntryIp6(IPAddress.IPv6Any));
        enrBuilder.WithEntry(EnrEntryKey.Tcp6, new EntryTcp6(30303));
        
        var enr = enrBuilder.Build();
        var multiAddress = BeaconClientUtility.ConvertToMultiAddress(enr);
        
        Assert.That(multiAddress.ToString().Contains("ip6"), Is.True);
    }
    
    [Test]
    public void ConvertToMultiAddress_ShouldReturnIfEnrIsNull()
    {
        var multiAddress = BeaconClientUtility.ConvertToMultiAddress(null);
        Assert.That(multiAddress, Is.Null);
    }

    [Test]
    public void GetForkDigestBytes_ShouldReturnCorrectForkDigest()
    {
        var syncProtocolOptions = new SyncProtocolOptions
        {
            Preset = SizePreset.MainnetPreset,
            GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
            GenesisTime = 1606824023,
        };
        
        Config.InitializeWithMainnet(); 
        Phase0Preset.InitializeWithMainnet(); 
        AltairPreset.InitializeWithMainnet(); 

        var forkDigest = BeaconClientUtility.GetForkDigestBytes(syncProtocolOptions);

        Assert.That(forkDigest, Is.Not.Null);
        Assert.That(forkDigest, Is.EqualTo(Convert.FromHexString("6A95A1A9")));
    }
    
    [Test]
    public void GetForkDigestString_ShouldReturnCorrectForkDigest()
    {
        var syncProtocolOptions = new SyncProtocolOptions
        {
            Preset = SizePreset.MainnetPreset,
            GenesisValidatorsRoot = Convert.FromHexString("4b363db94e286120d76eb905340fdd4e54bfe9f06bf33ff6cf5ad27f511bfe95"),
            GenesisTime = 1606824023,
        };
        
        Config.InitializeWithMainnet(); 
        Phase0Preset.InitializeWithMainnet(); 
        AltairPreset.InitializeWithMainnet(); 

        var forkDigest = BeaconClientUtility.GetForkDigestString(syncProtocolOptions);

        Assert.That(forkDigest, Is.Not.Null);
        Assert.That(forkDigest, Is.EqualTo("6a95a1a9"));
    }
}