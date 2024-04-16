using Lantern.Beacon.Utility;
using Lantern.Discv5.Enr;
using Multiformats.Address;

namespace Lantern.Beacon.Networking.Discovery;

public class Node(IEnr enr)
{
    public Multiaddress? Address { get; set; } = MultiAddressEnrConverter.ConvertToMultiAddress(enr);

    public IEnr Enr { get; set; } = enr;
}