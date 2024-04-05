using Lantern.Beacon.Networking.Discovery;
using Multiformats.Address;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public class PeerManager(IDiscoveryProtocol discoveryProtocol, IPeerFactory peerFactory) : IPeerManager
{
    public readonly ILocalPeer LocalPeer = peerFactory.Create(new Identity());
    
    public async Task InitAsync()
    {
        try
        {
            
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to start peer manager");
            throw;
        }
    }
    
    public void UpdateMultiAddress(Multiaddress newAddress)
    {
        LocalPeer.Address = newAddress;
    }
}