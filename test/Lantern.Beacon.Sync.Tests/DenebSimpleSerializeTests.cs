using System.Numerics;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using NUnit.Framework;
using SszSharp;

namespace Lantern.Beacon.Sync.Tests;

public class DenebSimpleSerializeTests : FileFixtureBase
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
    public void DenebExecutionPayloadHeader_Default_ShouldSerializeAndDeserializeCorrectly()
    {
        var data = Convert.FromHexString(
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000048020000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        var denebExecutionPayloadHeader = DenebExecutionPayloadHeader.CreateDefault();
        var serializedDenebExecutionPayloadHeader = DenebExecutionPayloadHeader.Serialize(denebExecutionPayloadHeader, _preset);
        var deserializedDenebExecutionPayloadHeader = DenebExecutionPayloadHeader.Deserialize(serializedDenebExecutionPayloadHeader, _preset);
        
        Assert.That(serializedDenebExecutionPayloadHeader, Is.EqualTo(data));
        Assert.That(deserializedDenebExecutionPayloadHeader, Is.EqualTo(denebExecutionPayloadHeader));
        Assert.That(deserializedDenebExecutionPayloadHeader.ParentHash, Is.EqualTo(denebExecutionPayloadHeader.ParentHash));
        Assert.That(deserializedDenebExecutionPayloadHeader.FeeRecipientAddress, Is.EqualTo(denebExecutionPayloadHeader.FeeRecipientAddress));
        Assert.That(deserializedDenebExecutionPayloadHeader.StateRoot, Is.EqualTo(denebExecutionPayloadHeader.StateRoot));
        Assert.That(deserializedDenebExecutionPayloadHeader.ReceiptsRoot, Is.EqualTo(denebExecutionPayloadHeader.ReceiptsRoot));
        Assert.That(deserializedDenebExecutionPayloadHeader.LogsBloom, Is.EqualTo(denebExecutionPayloadHeader.LogsBloom));
        Assert.That(deserializedDenebExecutionPayloadHeader.PrevRandoa, Is.EqualTo(denebExecutionPayloadHeader.PrevRandoa));
        Assert.That(deserializedDenebExecutionPayloadHeader.BlockNumber, Is.EqualTo(denebExecutionPayloadHeader.BlockNumber));
        Assert.That(deserializedDenebExecutionPayloadHeader.GasLimit, Is.EqualTo(denebExecutionPayloadHeader.GasLimit));
        Assert.That(deserializedDenebExecutionPayloadHeader.GasUsed, Is.EqualTo(denebExecutionPayloadHeader.GasUsed));
        Assert.That(deserializedDenebExecutionPayloadHeader.Timestamp, Is.EqualTo(denebExecutionPayloadHeader.Timestamp));
        Assert.That(deserializedDenebExecutionPayloadHeader.ExtraData, Is.EqualTo(denebExecutionPayloadHeader.ExtraData));
        Assert.That(deserializedDenebExecutionPayloadHeader.BaseFeePerGas, Is.EqualTo(denebExecutionPayloadHeader.BaseFeePerGas));
        Assert.That(deserializedDenebExecutionPayloadHeader.BlockHash, Is.EqualTo(denebExecutionPayloadHeader.BlockHash));
        Assert.That(deserializedDenebExecutionPayloadHeader.TransactionsRoot, Is.EqualTo(denebExecutionPayloadHeader.TransactionsRoot));
        Assert.That(deserializedDenebExecutionPayloadHeader.WithdrawalsRoot, Is.EqualTo(denebExecutionPayloadHeader.WithdrawalsRoot));
        Assert.That(deserializedDenebExecutionPayloadHeader.BlobGasUsed, Is.EqualTo(denebExecutionPayloadHeader.BlobGasUsed));
        Assert.That(deserializedDenebExecutionPayloadHeader.ExcessBlobGas, Is.EqualTo(denebExecutionPayloadHeader.ExcessBlobGas));
    }
    
    [Test]
    [TestCase("mainnet/deneb/ssz_static/ExecutionPayloadHeader/ssz_random/case_0/value.yaml")]
    public void DenebExecutionPayloadHeader_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var parentHash =TestUtility.HexToByteArray((string)yamlData["parent_hash"]);
        var feeRecipient = TestUtility.HexToByteArray((string)yamlData["fee_recipient"]);
        var stateRoot = TestUtility.HexToByteArray((string)yamlData["state_root"]);
        var receiptsRoot = TestUtility.HexToByteArray((string)yamlData["receipts_root"]);
        var logsBloom = TestUtility.HexToByteArray((string)yamlData["logs_bloom"]);
        var prevRandao = TestUtility.HexToByteArray((string)yamlData["prev_randao"]);
        var blockNumber = ulong.Parse((string)yamlData["block_number"]);
        var gasLimit = ulong.Parse((string)yamlData["gas_limit"]);
        var gasUsed = ulong.Parse((string)yamlData["gas_used"]);
        var timestamp = ulong.Parse((string)yamlData["timestamp"]);
        var extraData = Convert.FromHexString(((string)yamlData["extra_data"]).Remove(0, 2).ToLower());
        var baseFeePerGas = BigInteger.Parse((string)yamlData["base_fee_per_gas"]);
        var blockHash = TestUtility.HexToByteArray((string)yamlData["block_hash"]);
        var transactionsRoot = TestUtility.HexToByteArray((string)yamlData["transactions_root"]); 
        var withdrawalsRoot = TestUtility.HexToByteArray((string)yamlData["withdrawals_root"]);
        var blobGasUsed = ulong.Parse((string)yamlData["blob_gas_used"]);
        var excessBlobGas = ulong.Parse((string)yamlData["excess_blob_gas"]);
        var header = DenebExecutionPayloadHeader.CreateFrom(parentHash, feeRecipient, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot, blobGasUsed, excessBlobGas); 
        var deserializedHeader =  DenebExecutionPayloadHeader.Deserialize(DenebExecutionPayloadHeader.Serialize(header, _preset), _preset);
        
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
        Assert.That(Convert.ToHexString(deserializedHeader.WithdrawalsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["withdrawals_root"].Remove(0, 2)));
        Assert.That(deserializedHeader.BlobGasUsed, Is.EqualTo(ulong.Parse((string)yamlData["blob_gas_used"])));
        Assert.That(deserializedHeader.ExcessBlobGas, Is.EqualTo(ulong.Parse((string)yamlData["excess_blob_gas"])));
    }
    
    [Test]
    public void DenebLightClientHeader_Default_ShouldSerializeAndDeserializeCorrectly()
    {
        var data = Convert.FromHexString(
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000f400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000048020000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");
        var denebLightClientHeader = DenebLightClientHeader.CreateDefault();
        var serializedDenebLightClientHeader = DenebLightClientHeader.Serialize(denebLightClientHeader, _preset);
        var deserializedDenebLightClientHeader = DenebLightClientHeader.Deserialize(serializedDenebLightClientHeader, _preset);
        
        Assert.That(serializedDenebLightClientHeader, Is.EqualTo(data));
        Assert.That(deserializedDenebLightClientHeader, Is.EqualTo(denebLightClientHeader));
        Assert.That(deserializedDenebLightClientHeader.Beacon, Is.EqualTo(denebLightClientHeader.Beacon));
        Assert.That(deserializedDenebLightClientHeader.Execution, Is.EqualTo(denebLightClientHeader.Execution));
        Assert.That(deserializedDenebLightClientHeader.ExecutionBranch, Is.EqualTo(denebLightClientHeader.ExecutionBranch));
    }
    
    [Test]
    [TestCase("mainnet/deneb/ssz_static/LightClientHeader/ssz_random/case_0/value.yaml")]
    public void DenebLightClientHeader_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData; 
        var slot = ulong.Parse((string)yamlData["beacon"]["slot"]);
        var proposerIndex = ulong.Parse((string)yamlData["beacon"]["proposer_index"]);
        var parentRoot = TestUtility.HexToByteArray((string)yamlData["beacon"]["parent_root"]);
        var stateRoot = TestUtility.HexToByteArray((string)yamlData["beacon"]["state_root"]); 
        var bodyRoot = TestUtility.HexToByteArray((string)yamlData["beacon"]["body_root"]);
        var beacon = Phase0BeaconBlockHeader.CreateFrom(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        var parentHash = TestUtility.HexToByteArray((string)yamlData["execution"]["parent_hash"]);
        var feeRecipient = TestUtility.HexToByteArray((string)yamlData["execution"]["fee_recipient"]);
        var executionStateRoot = TestUtility.HexToByteArray((string)yamlData["execution"]["state_root"]);
        var receiptsRoot = TestUtility.HexToByteArray((string)yamlData["execution"]["receipts_root"]); 
        var logsBloom = TestUtility.HexToByteArray((string)yamlData["execution"]["logs_bloom"]);
        var prevRandao = TestUtility.HexToByteArray((string)yamlData["execution"]["prev_randao"]);
        var blockNumber = ulong.Parse((string)yamlData["execution"]["block_number"]);
        var gasLimit = ulong.Parse((string)yamlData["execution"]["gas_limit"]);
        var gasUsed = ulong.Parse((string)yamlData["execution"]["gas_used"]);
        var timestamp = ulong.Parse((string)yamlData["execution"]["timestamp"]);
        var extraData = Convert.FromHexString(((string)yamlData["execution"]["extra_data"]).Remove(0, 2).ToLower());
        var baseFeePerGas = BigInteger.Parse((string)yamlData["execution"]["base_fee_per_gas"]);
        var blockHash = TestUtility.HexToByteArray((string)yamlData["execution"]["block_hash"]);
        var transactionsRoot = TestUtility.HexToByteArray((string)yamlData["execution"]["transactions_root"]);
        var withdrawalsRoot = TestUtility.HexToByteArray((string)yamlData["execution"]["withdrawals_root"]);
        var blobGasUsed = ulong.Parse((string)yamlData["execution"]["blob_gas_used"]);
        var excessBlobGas = ulong.Parse((string)yamlData["execution"]["excess_blob_gas"]);
        var header = DenebExecutionPayloadHeader.CreateFrom(parentHash, feeRecipient, executionStateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot, blobGasUsed, excessBlobGas); 
        var executionBranch = new byte[Constants.ExecutionBranchDepth][];
        
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            executionBranch[i] = TestUtility.HexToByteArray((string)yamlData["execution_branch"][i]);
        }
        
        var lightClientHeader = DenebLightClientHeader.CreateFrom(beacon, header, executionBranch);
        var deserializedLightClientHeader = DenebLightClientHeader.Deserialize(DenebLightClientHeader.Serialize(lightClientHeader, _preset), _preset);
        
        Assert.That(deserializedLightClientHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["slot"])));
        Assert.That(deserializedLightClientHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["body_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.ParentHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["parent_hash"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.FeeRecipientAddress.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["fee_recipient"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.ReceiptsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["receipts_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.LogsBloom).ToLower(), Is.EqualTo((string)yamlData["execution"]["logs_bloom"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.PrevRandoa.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["prev_randao"].Remove(0, 2)));
        Assert.That(deserializedLightClientHeader.Execution.BlockNumber, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["block_number"])));
        Assert.That(deserializedLightClientHeader.Execution.GasLimit, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["gas_limit"])));
        Assert.That(deserializedLightClientHeader.Execution.GasUsed, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["gas_used"])));
        Assert.That(deserializedLightClientHeader.Execution.Timestamp, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["timestamp"])));
        Assert.That(deserializedLightClientHeader.Execution.ExtraData, Is.EqualTo(header.ExtraData));
        Assert.That(deserializedLightClientHeader.Execution.BaseFeePerGas, Is.EqualTo(BigInteger.Parse((string)yamlData["execution"]["base_fee_per_gas"])));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.BlockHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["block_hash"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.TransactionsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["transactions_root"].Remove(0, 2)));
        Assert.That(deserializedLightClientHeader.Execution.BlobGasUsed, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["blob_gas_used"])));
        Assert.That(deserializedLightClientHeader.Execution.ExcessBlobGas, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["excess_blob_gas"])));
        
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientHeader.ExecutionBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(executionBranch[i].AsSpan()).ToLower()));
        }
    }
}