using Lantern.Beacon.Networking.Encoding;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class VarintTests
{
    [Test]
    public void Encode_ShouldEncodeValueCorrectly()
    {
        var buffer = new byte[10];
        int offset = 0;
        
        Varint.Encode(300, buffer, ref offset);

        Assert.That(2, Is.EqualTo(offset)); 
        Assert.That(0xAC, Is.EqualTo(buffer[0])); 
        Assert.That(0x02, Is.EqualTo(buffer[1])); 
    }

    [Test]
    public void Decode_ShouldDecodeValueCorrectly()
    {
        var buffer = new byte[] { 0xAC, 0x02 };
        int offset = 0;

        int value = Varint.Decode(buffer, ref offset);

        Assert.That(300, Is.EqualTo(value));
        Assert.That(2, Is.EqualTo(offset)); 
    }

    [Test]
    public void Decode_ShouldThrowExceptionWhenVarIntTooLong()
    {
        var buffer = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };
        int offset = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() => Varint.Decode(buffer, ref offset));
    }

    [Test]
    public void GetSizeInBytes_ShouldReturnCorrectSize()
    {
        Assert.That(1, Is.EqualTo(Varint.GetSizeInBytes(127)));  
        Assert.That(2, Is.EqualTo(Varint.GetSizeInBytes(128)));  
        Assert.That(2, Is.EqualTo(Varint.GetSizeInBytes(300)));  
        Assert.That(5, Is.EqualTo(Varint.GetSizeInBytes(int.MaxValue)));  
    }

    [Test]
    public void RoundtripEncodeDecode_ShouldReturnOriginalValue()
    {
        var buffer = new byte[10];

        for (int i = 0; i < 1000; i++)
        {
            int value = new Random().Next(0, Int32.MaxValue);
            int preEncodeOffset = 0;
            Varint.Encode(value, buffer, ref preEncodeOffset);

            int postEncodeOffset = 0;
            int decodedValue = Varint.Decode(buffer, ref postEncodeOffset);
            
            Assert.That(value, Is.EqualTo(decodedValue));
            Assert.That(preEncodeOffset, Is.EqualTo(postEncodeOffset));
        }
    }
}