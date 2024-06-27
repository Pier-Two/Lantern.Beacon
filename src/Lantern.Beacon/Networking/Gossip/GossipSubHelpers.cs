using IronSnappy;

namespace Lantern.Beacon.Networking.Gossip;

public static class GossipSubHelpers
{
    public static byte[] EncodeGossipMessage(byte[] sszData)
    {
        throw new Exception();
    }
    
    public static byte[] DecodeGossipMessage(byte[] gossipMessage)
    {
        var decompressedData = Snappy.Decode(gossipMessage);
        throw new Exception();
    }
}