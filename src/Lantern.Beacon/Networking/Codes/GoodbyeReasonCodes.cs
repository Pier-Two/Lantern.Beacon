namespace Lantern.Beacon.Networking.Codes;

public enum GoodbyeReasonCodes
{
    ClientShutdown = 1,
    IrrelevantNetwork = 2,
    InternalFaultOrError = 3,
    UnableToVerify = 128,
    TooManyPeers = 129,
    PeerScoreTooLow = 250,
    Banned = 251
}