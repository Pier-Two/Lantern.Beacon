using System.Buffers;

namespace Libp2p.Protocols.Mplex;

public class MplexMessage
{
    public int StreamId { get; set; }
    
    public MplexMessageFlag Flag { get; set; }
    
    public ReadOnlySequence<byte> Data { get; set; }
}