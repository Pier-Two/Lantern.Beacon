using System.Text;
using SszSharp;

namespace Lantern.Beacon.Sync.Types.Ssz.Phase0;

public class ErrorMessage
{
    [SszElement(0, "List[uint8, 256]")]
    public List<byte> Message { get; init; }
    
    public bool Equals(ErrorMessage? other)
    {
        return other != null && Message.Equals(other.Message);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is ErrorMessage other)
        {
            return Equals(other);
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Message);
    }
    
    public static ErrorMessage CreateFrom(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);

        if (bytes.Length > 256)
        {
            bytes = bytes[..256];
        }
        var messageList = new List<byte>();
        
        messageList.AddRange(bytes);
        
        return new ErrorMessage
        {
            Message = messageList
        };
    }
    
    public static ErrorMessage CreateDefault()
    {
        var message = new List<byte>();
        
        for (var i = 0; i < 256; i++)
        {
            message.Add(0);
        }
        
        return new ErrorMessage
        {
            Message = message
        };
    }
    
    public static byte[] Serialize(ErrorMessage message)
    {
        return SszContainer.Serialize(message);
    }
    
    public static ErrorMessage Deserialize(byte[] data)
    {
        return SszContainer.Deserialize<ErrorMessage>(data).Item1;
    }
}