using System.Buffers;
using System.Text;
using Castle.Components.DictionaryAdapter.Xml;
using Cryptography.ECDSA;
using Lantern.Beacon.Networking.Encoding;
using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync.Types;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using NBitcoin.Secp256k1;
using Nethermind.Libp2p.Core;
using Nethermind.Libp2p.Core.Dto;
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
    
    [Test]
    public void SignatureGenTest()
    {
        var privateKeyBytes = Convert.FromHexString("8E38710E07E69E0026908AEDF3A2BBE48095B92469B03DA505CF2962815DD2C0");
        var privateKey = Context.Instance.CreateECPrivKey((ReadOnlySpan<byte>)privateKeyBytes);
        var message = Sha256Manager.GetHash(Convert.FromHexString(
            "6E6F6973652D6C69627032702D7374617469632D6B65793A0D64FE4CCCE0BF401E91CBA81232B9C42AA37B2911AD9F6CBC738F39F44F3A08"));
        var sigResult = privateKey.TrySignECDSA(Sha256Manager.GetHash(message), out var sig);
        var signature = sig.ToDER();
        Console.WriteLine(Convert.ToHexString(signature));
    }

    [Test]
    public void SignatureGenTest2()
    {
        var message = Convert.FromHexString("32EF");
        var privateKeyBytes = new byte[32];
        Random.Shared.NextBytes(privateKeyBytes);
        
        var identity = new Identity(privateKeyBytes, KeyType.Secp256K1);
        var signature = identity.Sign(message);
        var result = identity.VerifySignature(message, signature);
        
        Console.WriteLine(result);
    }
    
    [Test]
    public void VerificationTest()
    {
        var privateKeyBytes = Convert.FromHexString("AA7F168C6A97F1448A0D0CE3BA8AFE1D2E326174D8F2A56F5F75FE40BECA299B");
        var signature =
            Convert.FromHexString(
                "20DA364249F7963EF79AD99F806A56DCC00A03C0EEEF6CC43D14B3778343E43B2E04493F4F943360F3E5B58F61144E8D66970049717750194C1AFDA7AB5FBF5E");
        var message = Convert.FromHexString("6E6F6973652D6C69627032702D7374617469632D6B65793A7C31BFFEB61CD03FCF174FD1975086E784563744E9F65A3BB73FCAA653FE266F");
        var localIdentity = new Identity(privateKeyBytes, KeyType.Secp256K1);
        var result = localIdentity.VerifySignature(message, signature);
        
        Console.WriteLine(result);
        // var privateKey = Context.Instance.CreateECPrivKey((ReadOnlySpan<byte>)privateKeyBytes);
        // var publicKey = privateKey.CreatePubKey();
        // SecpECDSASignature.TryCreateFromCompact(signature, out var serailizedSignature);
        // var result = publicKey.SigVerify(serailizedSignature, message);
        // Console.WriteLine(result);
    }

    [Test]
    public void CheckLength()
    {
        // Lodestar
        Console.WriteLine(Convert.FromHexString("3045022100e4a636bebe38c2dc6d45427efc371395dcf5c481c9bed42de8a0a4591034d2050220789f40394cc3c57f7b6c3acd75904f7bf60a1eed4ad755b24f40ebefc80bb712").Length);
        // Lantern
        Console.WriteLine(Convert.FromHexString("64C56D83EDDC1447A602917A629234356AF0FD414A3B66DD4A32A8532D80ED04126A5F6D88A05D593781D90F6CC80FB39FBF55B2CB8E1D2A446CDDCAD6CD6EA5").Length);
    }

    [Test]
    public void TestAddress()
    {
        var multiaddress = Multiaddress.Decode("/ip4/127.0.0.1/tcp/60987/p2p/16Uiu2HAm2uXXFnCvWBqvqmwsovZ4eo7zR8w2EZnKeyMPDVAXH8e5");
        var bytes = GetFilteredBytes(multiaddress);

        foreach (var data in bytes)
        {
            Console.Write(data);
        }
    }
    
    public static byte[] GetFilteredBytes(Multiaddress multiaddress) 
    {
        // Remove P2P part from the multiaddress if needed
        multiaddress.Remove<P2P>();
        byte[] bytes = multiaddress.ToBytes();
        int len = bytes.Length;

        // If the array is too short, there's nothing to remove
        if (len < 4) return bytes;

        // Determine the position of the last two bytes
        int secondLastIndex = len - 2;

        // Initialize a memory-efficient list to collect the filtered bytes
        List<byte> filteredBytes = new List<byte>(len);

        for (int i = 0; i < len; i++) {
            // Skip the two zero bytes only if they are right before the last two bytes
            if (i == secondLastIndex - 2 && bytes[i] == 0 && bytes[i + 1] == 0) {
                i++; // Skip the next zero byte as well
            } else {
                filteredBytes.Add(bytes[i]);
            }
        }

        return filteredBytes.ToArray();
    }
    
}