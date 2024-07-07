using Lantern.Beacon.Networking.Codes;
using Lantern.Beacon.Networking.Gossip;
using Lantern.Beacon.Sync;

namespace Lantern.Beacon.Networking.Encoding;

public static class ReqRespHelpers
{
    public static byte[] EncodeRequest(byte[] sszData)
    {
        var compressedData = SnappyHelper.Compress(sszData);
        var sszLength = sszData.Length;
        var varintHeader = new byte[Varint.GetSizeInBytes(sszLength)];
        var offset = 0;
        
        Varint.Encode(sszLength, varintHeader, ref offset);
        
        var result = new byte[varintHeader.Length + compressedData.Length];
        Array.Copy(varintHeader, 0, result, 0, varintHeader.Length);
        Array.Copy(compressedData, 0, result, varintHeader.Length, compressedData.Length);

        return result;
    }
    
    public static byte[] EncodeResponse(byte[] sszData, ResponseCodes responseCode)
    {
        var compressedData = SnappyHelper.Compress(sszData);
        var sszLength = sszData.Length;
        var varintHeader = new byte[Varint.GetSizeInBytes(sszLength)];
        var offset = 0;
        
        Varint.Encode(sszLength, varintHeader, ref offset);
        
        var result = (byte)responseCode;
        var resultArray = new byte[1 + varintHeader.Length + compressedData.Length];
        var currentOffset = 0;
        
        resultArray[currentOffset++] = result;
        
        Array.Copy(varintHeader, 0, resultArray, currentOffset, varintHeader.Length);
        currentOffset += varintHeader.Length;
        Array.Copy(compressedData, 0, resultArray, currentOffset, compressedData.Length);

        return resultArray;
    }
    
    public static byte[] EncodeResponseChunk(byte[] sszData, byte[] contextBytes, ResponseCodes responseCode)
    {
        if (contextBytes.Length != Constants.ContextBytesLength)
        {
            throw new ArgumentException($"Context bytes length must be {Constants.ContextBytesLength} bytes.");
        }

        var compressedData = SnappyHelper.Compress(sszData);
        var sszLength = sszData.Length;
        var varintHeader = new byte[Varint.GetSizeInBytes(sszLength)];
        var offset = 0;
        
        Varint.Encode(sszLength, varintHeader, ref offset);
        
        byte result = (byte)responseCode;
        var resultArray = new byte[1 + contextBytes.Length + varintHeader.Length + compressedData.Length];
        var currentOffset = 0;
        resultArray[currentOffset++] = result;

        Array.Copy(contextBytes, 0, resultArray, currentOffset, contextBytes.Length);
        currentOffset += contextBytes.Length;

        Array.Copy(varintHeader, 0, resultArray, currentOffset, varintHeader.Length);
        currentOffset += varintHeader.Length;

        Array.Copy(compressedData, 0, resultArray, currentOffset, compressedData.Length);

        return resultArray;
    }
    
    public static byte[] DecodeRequest(byte[] encodedData) 
    { 
        var offset = 0;
        var sszLength = Varint.Decode(encodedData, ref offset);
        var compressedDataStart = offset;
        var compressedDataLength = encodedData.Length - compressedDataStart;
        var compressedData = new byte[compressedDataLength];
        
        Array.Copy(encodedData, compressedDataStart, compressedData, 0, compressedDataLength);
        
        var decompressedData = SnappyHelper.Decompress(compressedData);
        
        if (decompressedData.Length != sszLength) 
        { 
            throw new InvalidDataException("Decompressed data length does not match the length specified in the header."); 
        }

        return decompressedData; 
    }

    public static (byte[], ResponseCodes) DecodeResponse(byte[] encodedData)
    {
        var offset = 0;
        var responseCode = (ResponseCodes)encodedData[offset++];
        var sszLength = Varint.Decode(encodedData, ref offset);
        var compressedDataStart = offset;
        var compressedDataLength = encodedData.Length - compressedDataStart;
        var compressedData = new byte[compressedDataLength];
        
        Array.Copy(encodedData, compressedDataStart, compressedData, 0, compressedDataLength);
        
        var decompressedData = SnappyHelper.Decompress(compressedData);
        
        if (decompressedData.Length != sszLength)
        {
            throw new InvalidDataException("Decompressed data length does not match the length specified in the header.");
        }

        return (decompressedData, responseCode); 
    }
    
    public static (byte, byte[], byte[]) DecodeResponseChunk(byte[] encodedChunk)
    {
        var offset = 0;
        var result = encodedChunk[offset++];
        var contextBytes = new byte[Constants.ContextBytesLength];
        
        if (Constants.ContextBytesLength > 0)
        {
            Array.Copy(encodedChunk, offset, contextBytes, 0, Constants.ContextBytesLength);
            offset += Constants.ContextBytesLength;
        }
        
        var sszLength = Varint.Decode(encodedChunk, ref offset);
        var compressedDataStart = offset;
        var compressedDataLength = encodedChunk.Length - compressedDataStart;
        var compressedData = new byte[compressedDataLength];
        
        Array.Copy(encodedChunk, compressedDataStart, compressedData, 0, compressedDataLength);
        
        var decompressedData = SnappyHelper.Decompress(compressedData);
        
        if (decompressedData.Length != sszLength)
        {
            throw new InvalidDataException("Decompressed data length does not match the length specified in the header.");
        }

        return (result, contextBytes, decompressedData); 
    }
}
