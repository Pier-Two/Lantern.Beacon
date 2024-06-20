using Lantern.Beacon.Networking.Codes;
using Nethermind.Libp2p.Core;

namespace Lantern.Beacon.Networking;

public static class RequestHelpers
{
    public static byte[] EncodeRequest(byte[] sszPayload)
    {
        var compressedData = SnappyHelper.Compress(sszPayload);
        var lengthSize = VarInt.GetSizeInBytes(sszPayload.Length);
        var lengthBytes = new byte[lengthSize];
        var offset = 0;
        
        VarInt.Encode(sszPayload.Length, lengthBytes, ref offset);
        
        var prependedData = new byte[lengthBytes.Length + compressedData.Length];
        
        Buffer.BlockCopy(lengthBytes, 0, prependedData, 0, lengthBytes.Length);
        Buffer.BlockCopy(compressedData, 0, prependedData, lengthBytes.Length, compressedData.Length);

        return prependedData;
    }

    public static bool DecodeResponse(byte[] encodedSszPayload, out byte[] decodedSszPayload)
    {
        var varintOffset = 0;
        var responseCode = (ResponseCodes)encodedSszPayload.First();
        
        if(responseCode != ResponseCodes.Success)
        {
            decodedSszPayload = [];
            return false;
        }
        
        varintOffset++;
        
        var varintLength = (int)VarInt.Decode(encodedSszPayload, ref varintOffset);
        var decompressedData = SnappyHelper.Decompress(encodedSszPayload.AsSpan(varintOffset, encodedSszPayload.Length - varintOffset).ToArray());
        
        if(decompressedData.Length != varintLength)
        {
            decodedSszPayload = [];
            return false;
        }
        
        decodedSszPayload = decompressedData;
        return true;
    }
}