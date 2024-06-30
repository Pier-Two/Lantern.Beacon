namespace Lantern.Beacon.Networking.Libp2pProtocols.Mplex;

public enum MplexMessageFlag
{
    NewStream = 0,
    MessageReceiver = 1,
    MessageInitiator = 2,
    CloseReceiver = 3,
    CloseInitiator = 4,
    ResetReceiver = 5,
    ResetInitiator = 6
}