using Cortex.Containers;
using Nethermind.Crypto;

namespace Lantern.Beacon.Sync.Tests;

public static class TestUtility
{
    public static byte[] HexToByteArray(string hexString)
    {
        return Convert.FromHexString(hexString.Remove(0, 2));
    }
    
    public static Bytes32 HexToBytes32(string hexString) {
        return new Bytes32(Convert.FromHexString(hexString.Remove(0, 2)));
    }
}