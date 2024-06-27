namespace Lantern.Beacon.Networking.Encoding;

public static class Varint
{
    public static void Encode(int value, byte[] buffer, ref int offset)
    {
        while ((value & 0xFFFFFF80) != 0)
        {
            buffer[offset++] = (byte)((value & 0x7F) | 0x80);
            value >>= 7;
        }
        buffer[offset++] = (byte)(value & 0x7F);
    }
    
    public static int Decode(byte[] buffer, ref int offset)
    {
        int value = 0;
        int shift = 0;
        int b;
        do
        {
            if (shift >= 32)
                throw new ArgumentOutOfRangeException(nameof(buffer), "VarInt is too long");

            b = buffer[offset++];
            value |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return value;
    }
    
    public static int GetSizeInBytes(int value)
    {
        int size = 0;
        while ((value & 0xFFFFFF80) != 0)
        {
            size++;
            value >>= 7;
        }
        return size + 1; 
    }
}