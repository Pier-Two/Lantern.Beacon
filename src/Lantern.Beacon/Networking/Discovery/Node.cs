using Lantern.Beacon;
using Lantern.Discv5.Enr;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public class Node
{
    public Multiaddress? Address { get; set; }

    public IEnr Enr { get; set; } 
    
    public Node(IEnr enr)
    {
        Enr = enr;
        Address = BeaconClientUtility.ConvertToMultiAddress(enr);
    }
}