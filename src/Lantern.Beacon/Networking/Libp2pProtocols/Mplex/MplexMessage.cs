using System.Buffers;

namespace Lantern.Beacon.Networking.Libp2pProtocols.Mplex;

public class MplexMessage
{
    public long StreamId { get; set; }
    
    public MplexMessageFlag Flag { get; set; }
    
    public ReadOnlySequence<byte> Data { get; set; }
}