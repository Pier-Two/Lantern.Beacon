using System.Buffers;
using System.Text;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Nethermind.Libp2p.Core;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class CoreTests
{
    [Test]
    public void Test()
    {
        var str = "/eth2/beacon_chain/req/goodbye/1/ssz_snappy";
        int len = Encoding.UTF8.GetByteCount(str) + 1;
        byte[] buf = new byte[VarInt.GetSizeInBytes(len) + len];
        int offset = 0;
        VarInt.Encode(len, buf, ref offset);
        Encoding.UTF8.GetBytes(str, 0, str.Length, buf, offset);
        buf[^1] = 0x0a;
        var buffer = new ReadOnlySequence<byte>(buf);
        Console.WriteLine(Convert.ToHexString(buffer.ToArray()));
        
    }
}