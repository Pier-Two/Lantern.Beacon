using System.Net;
using Lantern.Beacon.Sync;
using Lantern.Beacon.Sync.Helpers;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Entries;
using Multiformats.Address;
using SszSharp;

namespace Lantern.Beacon;

public static class BeaconClientUtility
{
    public static byte[] GetForkDigestBytes(SyncProtocolOptions options)
    {
        var epoch = Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(options.GenesisTime));
        var currentForkVersion = Phase0Helpers.ComputeForkVersion(epoch);
        var forkDigest = Phase0Helpers.ComputeForkDigest(currentForkVersion, options);
        return forkDigest;
    }
    
    public static string GetForkDigestString(SyncProtocolOptions options)
    {
        var epoch = Phase0Helpers.ComputeEpochAtSlot(Phase0Helpers.ComputeCurrentSlot(options.GenesisTime));
        var currentForkVersion = Phase0Helpers.ComputeForkVersion(epoch);
        var forkDigest = Convert.ToHexString(Phase0Helpers.ComputeForkDigest(currentForkVersion, options)).ToLower();
        return forkDigest;
    }
    
    public static Multiaddress? ConvertToMultiAddress(IEnr? enr)
    {
        if (enr == null)
        {
            return null;
        }
        
        if (TryGetIpAndPort(enr, EnrEntryKey.Ip, EnrEntryKey.Tcp, out var ip, out var port))
        {
            return Multiaddress.Decode($"ip4/{ip}/tcp/{port}/p2p/{enr.ToPeerId()}");
        }
        
        if (TryGetIpAndPort(enr, EnrEntryKey.Ip6, EnrEntryKey.Tcp6, out ip, out port)) 
        {
            return Multiaddress.Decode($"ip6/{ip}/tcp/{port}/p2p/{enr.ToPeerId()}");
        }
        
        return null;
    }
    
    private static bool TryGetIpAndPort(IEnr enr, string ipKey, string portKey, out IPAddress? ip, out int port)
    {
        ip = null;
        port = 0;
        
        if (!enr.HasKey(ipKey) || !enr.HasKey(portKey))
        {
            return false;
        }
        
        ip = enr.GetEntry<EntryIp>(ipKey).Value;
        port = enr.GetEntry<EntryTcp>(portKey).Value;
        
        return true;
    }
}