using System.Collections;
using NUnit.Framework;
using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types;
using Nethermind.Core.Extensions;
using Nethermind.Int256;
using BeaconBlockHeader = Lantern.Beacon.SyncProtocol.Types.BeaconBlockHeader;

namespace Lantern.Beacon.SyncProtocol.Tests;

[TestFixture]
public class SimpleSerializeTests : YamlFixtureBase
{
    private string _dataFolderPath;
    
    [SetUp]
    public void Setup()
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(SimpleSerializeTests).Assembly.Location);
        var projectFolderPath = Directory.GetParent(assemblyFolder).Parent.Parent.FullName;
        _dataFolderPath = Path.Combine(projectFolderPath, "MockData");
    }
    
    [Test]
    [TestCase("mainnet/phase0/ssz_static/BeaconBlockHeader/ssz_random/case_0/value.yaml")]
    public void BeaconBlockHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
    
        var yamlData = _yamlData;
        var slot = new Slot(ulong.Parse((string)yamlData["slot"]));
        var proposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["proposer_index"]));
        var parentRoot = TestUtility.HexToBytes32((string)yamlData["parent_root"]);
        var stateRoot = TestUtility.HexToBytes32((string)yamlData["state_root"]);
        var bodyRoot = TestUtility.HexToBytes32((string)yamlData["body_root"]);
        var header = new BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        var deserializedHeader = BeaconBlockHeader.Serializer.Deserialize(BeaconBlockHeader.Serializer.Serialize(header).AsSpan());
        
        Assert.That((ulong)deserializedHeader.Slot, Is.EqualTo(ulong.Parse((string)yamlData["slot"])));
        Assert.That((ulong)deserializedHeader.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedHeader.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["body_root"].Remove(0, 2)));
    }
    
    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientHeader/ssz_random/case_0/value.yaml")]
    public void LightClientHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
    
        var yamlData = _yamlData;
        var slot = new Slot(ulong.Parse((string)yamlData["beacon"]["slot"]));
        var proposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["beacon"]["proposer_index"]));
        var parentRoot = TestUtility.HexToBytes32((string)yamlData["beacon"]["parent_root"]);
        var stateRoot = TestUtility.HexToBytes32((string)yamlData["beacon"]["state_root"]);
        var bodyRoot = TestUtility.HexToBytes32((string)yamlData["beacon"]["body_root"]);
        var header = new BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        var deserializedHeader = BeaconBlockHeader.Serializer.Deserialize(BeaconBlockHeader.Serializer.Serialize(header));
        
        Console.WriteLine(Convert.ToHexString(deserializedHeader.ParentRoot.AsSpan()).ToLower());
        Assert.That((ulong)deserializedHeader.Slot, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["slot"])));
        Assert.That((ulong)deserializedHeader.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedHeader.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["body_root"].Remove(0, 2)));
    }

    [Test]
    [TestCase("mainnet/altair/ssz_static/SyncCommittee/ssz_random/case_0/value.yaml")]
    public void SyncCommittee_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var pubkeysList = (List<object>)yamlData["pubkeys"];
        var pubKeysArray = new BlsPublicKey[pubkeysList.Count];
        
        foreach (var pubkey in pubkeysList)
        {
            pubKeysArray[pubkeysList.IndexOf(pubkey)] = new BlsPublicKey(TestUtility.HexToByteArray((string)pubkey));
        }
        
        var aggregatePubKey = new BlsPublicKey(TestUtility.HexToByteArray((string)yamlData["aggregate_pubkey"]));
        var syncCommittee = new SyncCommittee(pubKeysArray, aggregatePubKey);
        var deserializedSyncCommittee = SyncCommittee.Serializer.Deserialize(SyncCommittee.Serializer.Serialize(syncCommittee));
        
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            Assert.That(Convert.ToHexString(deserializedSyncCommittee.PubKeys[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(pubKeysArray[i].AsSpan()).ToLower()));
        }
        
        Assert.That(Convert.ToHexString(deserializedSyncCommittee.AggregatePubKey.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(aggregatePubKey.AsSpan()).ToLower()));
    }

    [Test]
    [TestCase("mainnet/altair/ssz_static/SyncAggregate/ssz_random/case_0/value.yaml")]
    public void SyncAggregate_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var syncCommitteeBits = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_committee_bits"]));
        var syncCommitteeSignature = new BlsSignature(TestUtility.HexToByteArray((string)yamlData["sync_committee_signature"]));
        var syncAggregate = new SyncAggregate(syncCommitteeBits, syncCommitteeSignature);
        var deserializedSyncAggregate = SyncAggregate.Serializer.Deserialize(SyncAggregate.Serializer.Serialize(syncAggregate));
        
        Assert.That(syncCommitteeBits, Is.EqualTo(deserializedSyncAggregate.SyncCommitteeBits));
        Assert.That(Convert.ToHexString(deserializedSyncAggregate.SyncCommitteeSignature.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(syncCommitteeSignature.AsSpan()).ToLower()));
    }

    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientBootstrap/ssz_random/case_0/value.yaml")]
    public void LightClientBootstrap_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var slot = new Slot(ulong.Parse((string)yamlData["header"]["beacon"]["slot"]));
        var proposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["header"]["beacon"]["proposer_index"]));
        var parentRoot = TestUtility.HexToBytes32((string)yamlData["header"]["beacon"]["parent_root"]);
        var stateRoot = TestUtility.HexToBytes32((string)yamlData["header"]["beacon"]["state_root"]);
        var bodyRoot = TestUtility.HexToBytes32((string)yamlData["header"]["beacon"]["body_root"]);
        var pubkeysList = (List<object>)yamlData["current_sync_committee"]["pubkeys"];
        var pubKeysArray = new BlsPublicKey[pubkeysList.Count];
        
        foreach (var pubkey in pubkeysList)
        {
            pubKeysArray[pubkeysList.IndexOf(pubkey)] = new BlsPublicKey(TestUtility.HexToByteArray((string)pubkey));
        }
        
        var aggregatePubKey = new BlsPublicKey(TestUtility.HexToByteArray((string)yamlData["current_sync_committee"]["aggregate_pubkey"]));
        var branch = new Bytes32[Constants.CurrentSyncCommitteeBranchDepth];
        
        for (var i = 0; i < Constants.CurrentSyncCommitteeBranchDepth; i++)
        {
            branch[i] = new Bytes32(TestUtility.HexToByteArray((string)yamlData["current_sync_committee_branch"][i]));
        }
        
        var header = new LightClientHeader(new BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot));
        var syncCommittee = new SyncCommittee(pubKeysArray, aggregatePubKey);
        var lightClientBootstrap = new LightClientBootstrap(header, syncCommittee, branch);
        var deserializedLightClientBootstrap = LightClientBootstrap.Serializer.Deserialize(LightClientBootstrap.Serializer.Serialize(lightClientBootstrap));
        
        Assert.That((ulong)deserializedLightClientBootstrap.Header.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["header"]["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientBootstrap.Header.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.Header.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.Header.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.Header.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.CurrentSyncCommittee.PubKeys[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(pubKeysArray[i].AsSpan()).ToLower()));
        }
        
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.CurrentSyncCommittee.AggregatePubKey.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(aggregatePubKey.AsSpan()).ToLower()));
        
        for (var i = 0; i < Constants.CurrentSyncCommitteeBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.CurrentSyncCommitteeBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(branch[i].AsSpan()).ToLower()));
        }
    }

    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientUpdate/ssz_random/case_0/value.yaml")]
    public void LightClientUpdate_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var attestedSlot = new Slot(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"]));
        var attestedProposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"]));
        var attestedParentRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["parent_root"]);
        var attestedStateRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["state_root"]);
        var attestedBodyRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["body_root"]);
        var pubkeysList = (List<object>)yamlData["next_sync_committee"]["pubkeys"];
        var pubKeysArray = new BlsPublicKey[pubkeysList.Count];
        
        foreach (var pubkey in pubkeysList)
        {
            pubKeysArray[pubkeysList.IndexOf(pubkey)] = new BlsPublicKey(TestUtility.HexToByteArray((string)pubkey));
        }
        
        var nextSyncCommitteebranch = new Bytes32[Constants.NextSyncCommitteeBranchDepth];
        
        for (var i = 0; i < Constants.NextSyncCommitteeBranchDepth; i++)
        {
            nextSyncCommitteebranch[i] = new Bytes32(TestUtility.HexToByteArray((string)yamlData["next_sync_committee_branch"][i]));
        }
        
        var finalizedSlot = new Slot(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"]));
        var finalizedProposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"]));
        var finalizedParentRoot = TestUtility.HexToBytes32((string)yamlData["finalized_header"]["beacon"]["parent_root"]);
        var finalizedStateRoot = TestUtility.HexToBytes32((string)yamlData["finalized_header"]["beacon"]["state_root"]);
        var finalizedBodyRoot = TestUtility.HexToBytes32((string)yamlData["finalized_header"]["beacon"]["body_root"]);
        var finalizedBranch = new Bytes32[Constants.FinalityBranchDepth];
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalizedBranch[i] = new Bytes32(TestUtility.HexToByteArray((string)yamlData["finality_branch"][i]));
        }
        
        var syncCommitteeBits = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_bits"]));
        var syncCommitteeSignature = new BlsSignature(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_signature"]));
        var signatureSlot = new Slot(ulong.Parse((string)yamlData["signature_slot"]));
        var attestedHeader = new LightClientHeader(new BeaconBlockHeader(attestedSlot, attestedProposerIndex, attestedParentRoot, attestedStateRoot, attestedBodyRoot));
        var aggregatePubKey = new BlsPublicKey(TestUtility.HexToByteArray((string)yamlData["next_sync_committee"]["aggregate_pubkey"]));
        var nextSyncCommittee = new SyncCommittee(pubKeysArray, aggregatePubKey);
        var finalizedHeader = new LightClientHeader(new BeaconBlockHeader(finalizedSlot, finalizedProposerIndex, finalizedParentRoot, finalizedStateRoot, finalizedBodyRoot));
        var syncAggregate = new SyncAggregate(syncCommitteeBits, syncCommitteeSignature);
        var lightClientUpdate = new LightClientUpdate(attestedHeader, nextSyncCommittee, nextSyncCommitteebranch, finalizedHeader, finalizedBranch, syncAggregate, signatureSlot);
        var deserializedLightClientUpdate = LightClientUpdate.Serializer.Deserialize(LightClientUpdate.Serializer.Serialize(lightClientUpdate));
        
        Assert.That((ulong)deserializedLightClientUpdate.AttestedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientUpdate.AttestedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.AttestedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.AttestedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.AttestedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientUpdate.NextSyncCommittee.PubKeys[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(pubKeysArray[i].AsSpan()).ToLower()));
        }
        
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.NextSyncCommittee.AggregatePubKey.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(aggregatePubKey.AsSpan()).ToLower()));
        
        for (var i = 0; i < Constants.NextSyncCommitteeBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientUpdate.NextSyncCommitteeBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(nextSyncCommitteebranch[i].AsSpan()).ToLower()));
        }
        
        Assert.That((ulong)deserializedLightClientUpdate.FinalizedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientUpdate.FinalizedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.FinalizedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.FinalizedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.FinalizedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientUpdate.FinalizedBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(finalizedBranch[i].AsSpan()).ToLower()));
        }
    }

    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientOptimisticUpdate/ssz_random/case_0/value.yaml")]
    public void LightClientOptimisticUpdate_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var attestedSlot = new Slot(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])); 
        var attestedProposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"]));
        var attestedParentRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["parent_root"]);
        var attestedStateRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["state_root"]);
        var attestedBodyRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["body_root"]);
        var syncCommitteeBits = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_bits"]));
        var syncCommitteeSignature = new BlsSignature(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_signature"]));
        var signatureSlot = new Slot(ulong.Parse((string)yamlData["signature_slot"]));
        var attestedHeader = new LightClientHeader(new BeaconBlockHeader(attestedSlot, attestedProposerIndex, attestedParentRoot, attestedStateRoot, attestedBodyRoot));
        var syncAggregate = new SyncAggregate(syncCommitteeBits, syncCommitteeSignature);
        var optimisticUpdate = new LightClientOptimisticUpdate(attestedHeader, syncAggregate, signatureSlot);
        var deserializedOptimisticUpdate = LightClientOptimisticUpdate.Serializer.Deserialize(LightClientOptimisticUpdate.Serializer.Serialize(optimisticUpdate));
        
        Assert.That((ulong)deserializedOptimisticUpdate.AttestedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])));
        Assert.That((ulong)deserializedOptimisticUpdate.AttestedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AttestedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AttestedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AttestedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["body_root"].Remove(0, 2)));
        Assert.That(syncCommitteeBits, Is.EqualTo(deserializedOptimisticUpdate.SyncAggregate.SyncCommitteeBits));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.SyncAggregate.SyncCommitteeSignature.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(syncCommitteeSignature.AsSpan()).ToLower()));
        Assert.That((ulong)deserializedOptimisticUpdate.SignatureSlot, Is.EqualTo(ulong.Parse((string)yamlData["signature_slot"])));
    }

    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientFinalityUpdate/ssz_random/case_0/value.yaml")]
    public void LightClientFinalityUpdate_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));

        var yamlData = _yamlData;
        var attestedSlot = new Slot(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"]));
        var attestedProposerIndex =
            new ValidatorIndex(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"]));
        var attestedParentRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["parent_root"]);
        var attestedStateRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["state_root"]);
        var attestedBodyRoot = TestUtility.HexToBytes32((string)yamlData["attested_header"]["beacon"]["body_root"]);
        var finalizedSlot = new Slot(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"]));
        var finalizedProposerIndex =
            new ValidatorIndex(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"]));
        var finalizedParentRoot = TestUtility.HexToBytes32((string)yamlData["finalized_header"]["beacon"]["parent_root"]);
        var finalizedStateRoot = TestUtility.HexToBytes32((string)yamlData["finalized_header"]["beacon"]["state_root"]);
        var finalizedBodyRoot = TestUtility.HexToBytes32((string)yamlData["finalized_header"]["beacon"]["body_root"]);
        var finalizedBranch = new Bytes32[Constants.FinalityBranchDepth];
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalizedBranch[i] = new Bytes32(TestUtility.HexToByteArray((string)yamlData["finality_branch"][i]));
        }
        
        var syncCommitteeBits =
            new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_bits"]));
        var syncCommitteeSignature =
            new BlsSignature(
                TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_signature"]));
        var signatureSlot = new Slot(ulong.Parse((string)yamlData["signature_slot"]));
        var attestedHeader = new LightClientHeader(
            new BeaconBlockHeader(attestedSlot, attestedProposerIndex, attestedParentRoot, attestedStateRoot,
                attestedBodyRoot));
        var finalizedHeader = new LightClientHeader(new BeaconBlockHeader(finalizedSlot, finalizedProposerIndex, finalizedParentRoot,
            finalizedStateRoot, finalizedBodyRoot));
        var syncAggregate = new SyncAggregate(syncCommitteeBits, syncCommitteeSignature);
        var lightClientFinalityUpdate = new LightClientFinalityUpdate(attestedHeader, finalizedHeader, finalizedBranch, syncAggregate, signatureSlot);
        var deserializedLightClientFinalityUpdate = LightClientFinalityUpdate.Serializer.Deserialize(LightClientFinalityUpdate.Serializer.Serialize(lightClientFinalityUpdate));
        
        Assert.That((ulong)deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["body_root"].Remove(0, 2)));
        Assert.That((ulong)deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalityBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(finalizedBranch[i].AsSpan()).ToLower()));
        }
        
        Assert.That(syncCommitteeBits, Is.EqualTo(deserializedLightClientFinalityUpdate.SyncAggregate.SyncCommitteeBits));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.SyncAggregate.SyncCommitteeSignature.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(syncCommitteeSignature.AsSpan()).ToLower()));
        Assert.That((ulong)deserializedLightClientFinalityUpdate.SignatureSlot, Is.EqualTo(ulong.Parse((string)yamlData["signature_slot"])));
    }

    [Test]
    [TestCase("mainnet/bellatrix/ssz_static/ExecutionPayloadHeader/ssz_random/case_0/value.yaml")]
    public void ExecutionPayloadHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var parentHash = new Hash32(TestUtility.HexToByteArray((string)yamlData["parent_hash"]));
        var feeRecipient = new Bytes20(TestUtility.HexToByteArray((string)yamlData["fee_recipient"]));
        var stateRoot = TestUtility.HexToBytes32((string)yamlData["state_root"]);
        var receiptsRoot = TestUtility.HexToBytes32((string)yamlData["receipts_root"]);
        var logsBloom = TestUtility.HexToByteArray((string)yamlData["logs_bloom"]);
        var prevRandao = TestUtility.HexToBytes32((string)yamlData["prev_randao"]);
        var blockNumber = ulong.Parse((string)yamlData["block_number"]);
        var gasLimit = ulong.Parse((string)yamlData["gas_limit"]);
        var gasUsed = ulong.Parse((string)yamlData["gas_used"]);
        var timestamp = ulong.Parse((string)yamlData["timestamp"]);
        var extraData = Convert.FromHexString(((string)yamlData["extra_data"]).Remove(0, 2).ToLower()).ToList();
        var baseFeePerGas = UInt256.Parse((string)yamlData["base_fee_per_gas"]);
        var blockHash = new Hash32(TestUtility.HexToByteArray((string)yamlData["block_hash"]));
        var transactionsRoot = TestUtility.HexToBytes32((string)yamlData["transactions_root"]); 
        var header = new ExecutionPayloadHeader(parentHash, feeRecipient, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot); 
        var deserializedHeader = ExecutionPayloadHeader.Serializer.Deserialize(ExecutionPayloadHeader.Serializer.Serialize(header));
        
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
        Assert.That(deserializedHeader.BaseFeePerGas, Is.EqualTo(UInt256.Parse((string)yamlData["base_fee_per_gas"])));
        Assert.That(Convert.ToHexString(deserializedHeader.BlockHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["block_hash"].Remove(0, 2))); 
        Assert.That(Convert.ToHexString(deserializedHeader.TransactionsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["transactions_root"].Remove(0, 2)));
    }
    
    [Test]
    [TestCase("mainnet/capella/ssz_static/ExecutionPayloadHeader/ssz_random/case_0/value.yaml")]
    public void CapellaExecutionPayloadHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var parentHash = new Hash32(TestUtility.HexToByteArray((string)yamlData["parent_hash"]));
        var feeRecipient = new Bytes20(TestUtility.HexToByteArray((string)yamlData["fee_recipient"]));
        var stateRoot = TestUtility.HexToBytes32((string)yamlData["state_root"]);
        var receiptsRoot = TestUtility.HexToBytes32((string)yamlData["receipts_root"]);
        var logsBloom = TestUtility.HexToByteArray((string)yamlData["logs_bloom"]);
        var prevRandao = TestUtility.HexToBytes32((string)yamlData["prev_randao"]);
        var blockNumber = ulong.Parse((string)yamlData["block_number"]);
        var gasLimit = ulong.Parse((string)yamlData["gas_limit"]);
        var gasUsed = ulong.Parse((string)yamlData["gas_used"]);
        var timestamp = ulong.Parse((string)yamlData["timestamp"]);
        var extraData = Convert.FromHexString(((string)yamlData["extra_data"]).Remove(0, 2).ToLower()).ToList();
        var baseFeePerGas = UInt256.Parse((string)yamlData["base_fee_per_gas"]);
        var blockHash = new Hash32(TestUtility.HexToByteArray((string)yamlData["block_hash"]));
        var transactionsRoot = TestUtility.HexToBytes32((string)yamlData["transactions_root"]); 
        var withdrawalsRoot = TestUtility.HexToBytes32((string)yamlData["withdrawals_root"]);
        var header = new CapellaExecutionPayloadHeader(parentHash, feeRecipient, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot); 
        var deserializedHeader = CapellaExecutionPayloadHeader.Serializer.Deserialize(CapellaExecutionPayloadHeader.Serializer.Serialize(header));
        
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
        Assert.That(deserializedHeader.BaseFeePerGas, Is.EqualTo(UInt256.Parse((string)yamlData["base_fee_per_gas"])));
        Assert.That(Convert.ToHexString(deserializedHeader.BlockHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["block_hash"].Remove(0, 2))); 
        Assert.That(Convert.ToHexString(deserializedHeader.TransactionsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["transactions_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.WithdrawalsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["withdrawals_root"].Remove(0, 2)));
    }
    
    [Test]
    [TestCase("mainnet/capella/ssz_static/LightClientHeader/ssz_random/case_0/value.yaml")]
    public void CapellaLightClientHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData; 
        var slot = new Slot(ulong.Parse((string)yamlData["beacon"]["slot"]));
        var proposerIndex = new ValidatorIndex(ulong.Parse((string)yamlData["beacon"]["proposer_index"]));
        var parentRoot = TestUtility.HexToBytes32((string)yamlData["beacon"]["parent_root"]);
        var stateRoot = TestUtility.HexToBytes32((string)yamlData["beacon"]["state_root"]); 
        var bodyRoot = TestUtility.HexToBytes32((string)yamlData["beacon"]["body_root"]);
        var beacon = new BeaconBlockHeader(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        var parentHash = new Hash32(TestUtility.HexToByteArray((string)yamlData["execution"]["parent_hash"]));
        var feeRecipient = new Bytes20(TestUtility.HexToByteArray((string)yamlData["execution"]["fee_recipient"]));
        var executionStateRoot = TestUtility.HexToBytes32((string)yamlData["execution"]["state_root"]);
        var receiptsRoot = TestUtility.HexToBytes32((string)yamlData["execution"]["receipts_root"]); 
        var logsBloom = TestUtility.HexToByteArray((string)yamlData["execution"]["logs_bloom"]);
        var prevRandao = TestUtility.HexToBytes32((string)yamlData["execution"]["prev_randao"]);
        var blockNumber = ulong.Parse((string)yamlData["execution"]["block_number"]);
        var gasLimit = ulong.Parse((string)yamlData["execution"]["gas_limit"]);
        var gasUsed = ulong.Parse((string)yamlData["execution"]["gas_used"]);
        var timestamp = ulong.Parse((string)yamlData["execution"]["timestamp"]);
        var extraData = Convert.FromHexString(((string)yamlData["execution"]["extra_data"]).Remove(0, 2).ToLower()).ToList();
        var baseFeePerGas = UInt256.Parse((string)yamlData["execution"]["base_fee_per_gas"]);
        var blockHash = new Hash32(TestUtility.HexToByteArray((string)yamlData["execution"]["block_hash"]));
        var transactionsRoot = TestUtility.HexToBytes32((string)yamlData["execution"]["transactions_root"]);
        var withdrawalsRoot = TestUtility.HexToBytes32((string)yamlData["execution"]["withdrawals_root"]);
        var execution = new CapellaExecutionPayloadHeader(parentHash, feeRecipient, executionStateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot);
        var executionBranch = new Bytes32[Constants.ExecutionBranchDepth];
        
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            executionBranch[i] = new Bytes32(TestUtility.HexToByteArray((string)yamlData["execution_branch"][i]));
        }
        
        var lightClientHeader = new CapellaLightClientHeader(beacon, execution, executionBranch);
        var deserializedLightClientHeader = CapellaLightClientHeader.Serializer.Deserialize(CapellaLightClientHeader.Serializer.Serialize(lightClientHeader));
        
        Assert.That((ulong)deserializedLightClientHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["proposer_index"])));
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
        Assert.That(deserializedLightClientHeader.Execution.ExtraData, Is.EqualTo(execution.ExtraData));
        Assert.That(deserializedLightClientHeader.Execution.BaseFeePerGas, Is.EqualTo(UInt256.Parse((string)yamlData["execution"]["base_fee_per_gas"])));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.BlockHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["block_hash"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Execution.TransactionsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["transactions_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientHeader.ExecutionBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(executionBranch[i].AsSpan()).ToLower()));
        }
    }
}