using System.IO.Compression;
using Snappier;

namespace Lantern.Beacon;

public static class SnappyHelper
{
    public static byte[] Compress(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var compressedStream = new MemoryStream();
        using (var snappyStream = new SnappyStream(compressedStream, CompressionMode.Compress, true))
        {
            inputStream.CopyTo(snappyStream);
        }
        return compressedStream.ToArray();
    }
    
    public static byte[] Decompress(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var decompressedStream = new MemoryStream();
        using (var snappyStream = new SnappyStream(compressedStream, CompressionMode.Decompress))
        {
            var buffer = new byte[65536];
            int bytesRead;
            while ((bytesRead = snappyStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                decompressedStream.Write(buffer, 0, bytesRead);
            }
        }
        return decompressedStream.ToArray();
    }

}