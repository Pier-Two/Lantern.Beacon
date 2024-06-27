using SszSharp;

namespace Lantern.Beacon.Sync.Types.Phase0;

public class Phase0BeaconBlockBody : IEquatable<Phase0BeaconBlockBody>
{
    [SszElement(0, "Vector[uint8, 96]")]
    public byte[] RandaoReveal { get; private init; } 
    
    [SszElement(1, "Container")]
    public Phase0Eth1Data Eth1Data { get; private init; }
    
    [SszElement(2, "Vector[uint8, 32]")]
    public byte[] Graffiti { get; private init; }
    
    [SszElement(3, "List[Container, MAX_PROPOSER_SLASHINGS]")]
    public byte[] ProposerSlashings { get; protected init; } 
    
    [SszElement(4, "List[Container, MAX_ATTESTER_SLASHINGS]")]
    public byte[] AttesterSlashings { get; protected init; }
    
    [SszElement(5, "List[Container, MAX_ATTESTATIONS]")]
    
    
    
    public bool Equals(Phase0BeaconBlockBody? other)
    {
        return other != null && RandaoReveal.SequenceEqual(other.RandaoReveal) && Eth1Data.Equals(other.Eth1Data) && Graffiti.Equals(other.Graffiti);
    }
}