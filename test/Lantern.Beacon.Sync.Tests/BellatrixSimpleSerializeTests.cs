using System.Numerics;
using Lantern.Beacon.Sync.Presets;
using NUnit.Framework;
using Lantern.Beacon.Sync.Types.Bellatrix;
using SszSharp;

namespace Lantern.Beacon.Sync.Tests;

[TestFixture]
public class BellatrixSimpleSerializeTests : FileFixtureBase
{
    private string _dataFolderPath;
    private SizePreset _preset;
    
    [SetUp]
    public void Setup()
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(CapellaSimpleSerializeTests).Assembly.Location);
        var projectFolderPath = Directory.GetParent(assemblyFolder).Parent.Parent.FullName;
        _dataFolderPath = Path.Combine(projectFolderPath, "MockData");
        _preset = SizePreset.MainnetPreset;
        Phase0Preset.InitializeWithMainnet();
        AltairPreset.InitializeWithMainnet();
    }
    
    [Test]
    public void BellatrixExecutionPayloadHeader_Default_ShouldSerializeAndDeserializeCorrectly()
    {
        var data = Convert.FromHexString(
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000018020000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        var executionPayloadHeader = BellatrixExecutionPayloadHeader.CreateDefault();
        var serializedExecutionPayloadHeader = BellatrixExecutionPayloadHeader.Serialize(executionPayloadHeader, _preset);
        var deserializedExecutionPayloadHeader = BellatrixExecutionPayloadHeader.Deserialize(serializedExecutionPayloadHeader, _preset);
        
        Assert.That(serializedExecutionPayloadHeader, Is.EqualTo(data));
        Assert.That(deserializedExecutionPayloadHeader.ParentHash, Is.EqualTo(executionPayloadHeader.ParentHash));
        Assert.That(deserializedExecutionPayloadHeader.FeeRecipientAddress, Is.EqualTo(executionPayloadHeader.FeeRecipientAddress));
        Assert.That(deserializedExecutionPayloadHeader.StateRoot, Is.EqualTo(executionPayloadHeader.StateRoot));
        Assert.That(deserializedExecutionPayloadHeader.ReceiptsRoot, Is.EqualTo(executionPayloadHeader.ReceiptsRoot));
        Assert.That(deserializedExecutionPayloadHeader.LogsBloom, Is.EqualTo(executionPayloadHeader.LogsBloom));
        Assert.That(deserializedExecutionPayloadHeader.PrevRandoa, Is.EqualTo(executionPayloadHeader.PrevRandoa));
        Assert.That(deserializedExecutionPayloadHeader.BlockNumber, Is.EqualTo(executionPayloadHeader.BlockNumber));
        Assert.That(deserializedExecutionPayloadHeader.GasLimit, Is.EqualTo(executionPayloadHeader.GasLimit));
        Assert.That(deserializedExecutionPayloadHeader.GasUsed, Is.EqualTo(executionPayloadHeader.GasUsed));
        Assert.That(deserializedExecutionPayloadHeader.Timestamp, Is.EqualTo(executionPayloadHeader.Timestamp));
        Assert.That(deserializedExecutionPayloadHeader.ExtraData, Is.EqualTo(executionPayloadHeader.ExtraData));
        Assert.That(deserializedExecutionPayloadHeader.BaseFeePerGas, Is.EqualTo(executionPayloadHeader.BaseFeePerGas));
        Assert.That(deserializedExecutionPayloadHeader.BlockHash, Is.EqualTo(executionPayloadHeader.BlockHash));
        Assert.That(deserializedExecutionPayloadHeader.TransactionsRoot, Is.EqualTo(executionPayloadHeader.TransactionsRoot));
    }
    
    [Test]
    [TestCase("mainnet/bellatrix/ssz_static/ExecutionPayloadHeader/ssz_random/case_0/value.yaml")]
    public void BellatrixExecutionPayloadHeader_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var parentHash = TestUtility.HexToByteArray((string)yamlData["parent_hash"]);
        var feeRecipient = TestUtility.HexToByteArray((string)yamlData["fee_recipient"]);
        var stateRoot = TestUtility.HexToByteArray((string)yamlData["state_root"]);
        var receiptsRoot = TestUtility.HexToByteArray((string)yamlData["receipts_root"]);
        var logsBloom = TestUtility.HexToByteArray((string)yamlData["logs_bloom"]);
        var prevRandao = TestUtility.HexToByteArray((string)yamlData["prev_randao"]);
        var blockNumber = ulong.Parse((string)yamlData["block_number"]);
        var gasLimit = ulong.Parse((string)yamlData["gas_limit"]);
        var gasUsed = ulong.Parse((string)yamlData["gas_used"]);
        var timestamp = ulong.Parse((string)yamlData["timestamp"]);
        var extraData = TestUtility.HexToByteArray(((string)yamlData["extra_data"]).Remove(0, 2).ToLower());
        var baseFeePerGas = BigInteger.Parse((string)yamlData["base_fee_per_gas"]);
        var blockHash = TestUtility.HexToByteArray((string)yamlData["block_hash"]);
        var transactionsRoot = TestUtility.HexToByteArray((string)yamlData["transactions_root"]); 
        var header = BellatrixExecutionPayloadHeader.CreateFrom(parentHash, feeRecipient, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot); 
        var deserializedHeader = BellatrixExecutionPayloadHeader.Deserialize(BellatrixExecutionPayloadHeader.Serialize(header, _preset), _preset);
        var bytes = BellatrixExecutionPayloadHeader.Serialize(header, _preset);
        Console.WriteLine(Convert.ToHexString(bytes).ToLower());
        
        Assert.That(Convert.ToHexString(deserializedHeader.ParentHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["parent_hash"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.FeeRecipientAddress.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["fee_recipient"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.ReceiptsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["receipts_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.LogsBloom).ToLower(), Is.EqualTo((string)yamlData["logs_bloom"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.PrevRandoa.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["prev_randao"].Remove(0, 2)));
        Assert.That(deserializedHeader.BlockNumber, Is.EqualTo(ulong.Parse((string)yamlData["block_number"])));
        Assert.That(deserializedHeader.GasLimit, Is.EqualTo(ulong.Parse((string)yamlData["gas_limit"])));
        Assert.That(deserializedHeader.GasUsed, Is.EqualTo(ulong.Parse((string)yamlData["gas_used"])));
        Assert.That(deserializedHeader.Timestamp, Is.EqualTo(ulong.Parse((string)yamlData["timestamp"])));
        Assert.That(deserializedHeader.ExtraData, Is.EqualTo(header.ExtraData));
        Assert.That(deserializedHeader.BaseFeePerGas, Is.EqualTo(BigInteger.Parse((string)yamlData["base_fee_per_gas"])));
        Assert.That(Convert.ToHexString(deserializedHeader.BlockHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["block_hash"].Remove(0, 2))); 
        Assert.That(Convert.ToHexString(deserializedHeader.TransactionsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["transactions_root"].Remove(0, 2)));
    }
}