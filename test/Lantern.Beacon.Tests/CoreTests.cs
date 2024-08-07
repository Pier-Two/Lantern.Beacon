using System.Buffers;
using System.Text;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;
using Nethermind.Libp2p.Core;
using NUnit.Framework;
using SszSharp;

namespace Lantern.Beacon.Tests;

[TestFixture]
public class CoreTests
{
    [Test]
    public void Test()
    {
        var data = Convert.FromHexString(
            "54FF060000734E6150705900370000455DEC3E54106A95A1A9009A01009C4D611D5B93FDAB69013A7F0A2F961CACA0C853F87CFE9595FE500381630793600000000000000000");
        var result = ReqRespHelpers.DecodeRequest(data);
        var status = Status.Deserialize(result);
        Console.WriteLine(status.FinalizedEpoch);
    }
    
}