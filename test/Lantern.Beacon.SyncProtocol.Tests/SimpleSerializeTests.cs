using System.Collections;
using System.Numerics;
using NUnit.Framework;
using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types;
using Lantern.Beacon.SyncProtocol.Types.Altair;
using Lantern.Beacon.SyncProtocol.Types.Bellatrix;
using Lantern.Beacon.SyncProtocol.Types.Capella;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using Nethermind.Int256;
using SszSharp;

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
        var slot = ulong.Parse((string)yamlData["slot"]);
        var proposerIndex =ulong.Parse((string)yamlData["proposer_index"]);
        var parentRoot = TestUtility.HexToByteArray((string)yamlData["parent_root"]);
        var stateRoot = TestUtility.HexToByteArray((string)yamlData["state_root"]);
        var bodyRoot = TestUtility.HexToByteArray((string)yamlData["body_root"]);
        var header = Phase0BeaconBlockHeader.CreateFrom(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        var deserializedHeader = Phase0BeaconBlockHeader.Deserialize(Phase0BeaconBlockHeader.Serialize(header));
        var containerType = SszContainer.GetContainer<Phase0BeaconBlockHeader>();
        var chunks = Merkleizer.GetChunks(containerType, header, new long[]{2, 3, 4});

        foreach (var chunk in chunks)
        {
            Console.WriteLine(Convert.ToHexString(chunk.AsSpan()).ToLower());
        }

        Assert.That(deserializedHeader.Slot, Is.EqualTo(ulong.Parse((string)yamlData["slot"])));
        Assert.That(deserializedHeader.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["proposer_index"])));
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
        var slot = ulong.Parse((string)yamlData["beacon"]["slot"]);
        var proposerIndex = ulong.Parse((string)yamlData["beacon"]["proposer_index"]);
        var parentRoot = TestUtility.HexToByteArray((string)yamlData["beacon"]["parent_root"]);
        var stateRoot = TestUtility.HexToByteArray((string)yamlData["beacon"]["state_root"]);
        var bodyRoot = TestUtility.HexToByteArray((string)yamlData["beacon"]["body_root"]);
        var header = Phase0BeaconBlockHeader.CreateFrom(slot, proposerIndex, parentRoot, stateRoot, bodyRoot);
        var lightClientHeader = AltairLightClientHeader.CreateFrom(header);
        var deserializedHeader =AltairLightClientHeader.Deserialize(AltairLightClientHeader.Serialize(lightClientHeader));
        
        Assert.That(deserializedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["slot"])));
        Assert.That(deserializedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["body_root"].Remove(0, 2)));
    }
    
    [Test]
    [TestCase("mainnet/altair/ssz_static/SyncCommittee/ssz_random/case_0/value.yaml")]
    public void SyncCommittee_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var pubkeysList = (List<object>)yamlData["pubkeys"];
        var pubKeysArray = new byte[pubkeysList.Count][];
        
        foreach (var pubkey in pubkeysList)
        {
            pubKeysArray[pubkeysList.IndexOf(pubkey)] = TestUtility.HexToByteArray((string)pubkey);
        }
        
        var aggregatePubKey = TestUtility.HexToByteArray((string)yamlData["aggregate_pubkey"]);
        var syncCommittee = AltairSyncCommittee.CreateFrom(pubKeysArray, aggregatePubKey);
        var deserializedSyncCommittee = AltairSyncCommittee.Deserialize(AltairSyncCommittee.Serialize(syncCommittee));
        
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
        var syncCommitteeBitsArray = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_committee_bits"]));
        var syncCommitteeBits = syncCommitteeBitsArray.Cast<bool>().ToList();
        var syncCommitteeSignature = TestUtility.HexToByteArray((string)yamlData["sync_committee_signature"]);
        var syncAggregate = AltairSyncAggregate.CreateFrom(syncCommitteeBits, syncCommitteeSignature);
        var deserializedSyncAggregate = AltairSyncAggregate.Deserialize(AltairSyncAggregate.Serialize(syncAggregate));
        Assert.That(syncCommitteeBits, Is.EqualTo(deserializedSyncAggregate.SyncCommitteeBits));
        Assert.That(Convert.ToHexString(deserializedSyncAggregate.SyncCommitteeSignature.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(syncCommitteeSignature.AsSpan()).ToLower()));
    }
    
    
    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientBootstrap/ssz_random/case_0/value.yaml")]
    public void LightClientBootstrap_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var slot = ulong.Parse((string)yamlData["header"]["beacon"]["slot"]);
        var proposerIndex = ulong.Parse((string)yamlData["header"]["beacon"]["proposer_index"]);
        var parentRoot = TestUtility.HexToByteArray((string)yamlData["header"]["beacon"]["parent_root"]);
        var stateRoot = TestUtility.HexToByteArray((string)yamlData["header"]["beacon"]["state_root"]);
        var bodyRoot = TestUtility.HexToByteArray((string)yamlData["header"]["beacon"]["body_root"]);
        var pubkeysList = (List<object>)yamlData["current_sync_committee"]["pubkeys"];
        var pubKeysArray = new byte[pubkeysList.Count][];
        
        foreach (var pubkey in pubkeysList)
        {
            pubKeysArray[pubkeysList.IndexOf(pubkey)] = TestUtility.HexToByteArray((string)pubkey);
        }
        
        var aggregatePubKey = TestUtility.HexToByteArray((string)yamlData["current_sync_committee"]["aggregate_pubkey"]);
        var branch = new byte[Constants.CurrentSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < Constants.CurrentSyncCommitteeBranchDepth; i++)
        {
            branch[i] = TestUtility.HexToByteArray((string)yamlData["current_sync_committee_branch"][i]);
        }
        
        var header = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(slot, proposerIndex, parentRoot, stateRoot, bodyRoot));
        var syncCommittee = AltairSyncCommittee.CreateFrom(pubKeysArray, aggregatePubKey);
        var lightClientBootstrap = AltairLightClientBootstrap.CreateFrom(header, syncCommittee, branch);
  
        var deserializedLightClientBootstrap = AltairLightClientBootstrap.Deserialize(AltairLightClientBootstrap.Serialize(lightClientBootstrap));
        
        Assert.That(deserializedLightClientBootstrap.Header.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["header"]["beacon"]["slot"])));
        Assert.That(deserializedLightClientBootstrap.Header.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.Header.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.Header.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.Header.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.CurrentAltairSyncCommittee.PubKeys[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(pubKeysArray[i].AsSpan()).ToLower()));
        }
        
        Assert.That(Convert.ToHexString(deserializedLightClientBootstrap.CurrentAltairSyncCommittee.AggregatePubKey.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(aggregatePubKey.AsSpan()).ToLower()));
        
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
        var attestedSlot = ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"]);
        var attestedProposerIndex = ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"]);
        var attestedParentRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["parent_root"]);
        var attestedStateRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["state_root"]);
        var attestedBodyRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["body_root"]);
        var pubkeysList = (List<object>)yamlData["next_sync_committee"]["pubkeys"];
        var pubKeysArray = new byte[pubkeysList.Count][];
        
        foreach (var pubkey in pubkeysList)
        {
            pubKeysArray[pubkeysList.IndexOf(pubkey)] = TestUtility.HexToByteArray((string)pubkey);
        }
        
        var nextSyncCommitteebranch = new byte[Constants.NextSyncCommitteeBranchDepth][];
        
        for (var i = 0; i < Constants.NextSyncCommitteeBranchDepth; i++)
        {
            nextSyncCommitteebranch[i] = TestUtility.HexToByteArray((string)yamlData["next_sync_committee_branch"][i]);
        }
        
        var finalizedSlot = ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"]);
        var finalizedProposerIndex = ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"]);
        var finalizedParentRoot = TestUtility.HexToByteArray((string)yamlData["finalized_header"]["beacon"]["parent_root"]);
        var finalizedStateRoot = TestUtility.HexToByteArray((string)yamlData["finalized_header"]["beacon"]["state_root"]);
        var finalizedBodyRoot = TestUtility.HexToByteArray((string)yamlData["finalized_header"]["beacon"]["body_root"]);
        var finalizedBranch = new byte[Constants.FinalityBranchDepth][];
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalizedBranch[i] = TestUtility.HexToByteArray((string)yamlData["finality_branch"][i]);
        }
        
        var syncCommitteeBitsArray = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_bits"]));
        var syncCommitteeBits = syncCommitteeBitsArray.Cast<bool>().ToList();
        var syncCommitteeSignature = TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_signature"]);
        var signatureSlot = ulong.Parse((string)yamlData["signature_slot"]);
        var attestedHeader = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(attestedSlot, attestedProposerIndex, attestedParentRoot, attestedStateRoot, attestedBodyRoot));
        var aggregatePubKey = TestUtility.HexToByteArray((string)yamlData["next_sync_committee"]["aggregate_pubkey"]);
        var nextSyncCommittee = AltairSyncCommittee.CreateFrom(pubKeysArray, aggregatePubKey);
        var finalizedHeader = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(finalizedSlot, finalizedProposerIndex, finalizedParentRoot, finalizedStateRoot, finalizedBodyRoot));
        var syncAggregate = AltairSyncAggregate.CreateFrom(syncCommitteeBits, syncCommitteeSignature);
        var lightClientUpdate = AltairLightClientUpdate.CreateFrom(attestedHeader, nextSyncCommittee, nextSyncCommitteebranch, finalizedHeader, finalizedBranch, syncAggregate, signatureSlot);
        AltairLightClientUpdate.Serialize(lightClientUpdate);
        var deserializedLightClientUpdate = AltairLightClientUpdate.Deserialize(AltairLightClientUpdate.Serialize(lightClientUpdate));
        
        Assert.That(deserializedLightClientUpdate.AttestedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])));
        Assert.That(deserializedLightClientUpdate.AttestedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.AttestedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.AttestedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.AttestedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.SyncCommitteeSize; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientUpdate.NextAltairSyncCommittee.PubKeys[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(pubKeysArray[i].AsSpan()).ToLower()));
        }
        
        Assert.That(Convert.ToHexString(deserializedLightClientUpdate.NextAltairSyncCommittee.AggregatePubKey.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(aggregatePubKey.AsSpan()).ToLower()));
        
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
            Assert.That(Convert.ToHexString(deserializedLightClientUpdate.FinalityBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(finalizedBranch[i].AsSpan()).ToLower()));
        }
    }
    
    
    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientOptimisticUpdate/ssz_random/case_0/value.yaml")]
    public void LightClientOptimisticUpdate_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
        var yamlData = _yamlData;
        var attestedSlot = ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"]); 
        var attestedProposerIndex = ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"]);
        var attestedParentRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["parent_root"]);
        var attestedStateRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["state_root"]);
        var attestedBodyRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["body_root"]);
        var syncCommitteeBitsArray = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_bits"]));
        var syncCommitteeBits = syncCommitteeBitsArray.Cast<bool>().ToList();
        var syncCommitteeSignature = TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_signature"]);
        var signatureSlot = ulong.Parse((string)yamlData["signature_slot"]);
        var attestedHeader = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(attestedSlot, attestedProposerIndex, attestedParentRoot, attestedStateRoot, attestedBodyRoot));
        var syncAggregate = AltairSyncAggregate.CreateFrom(syncCommitteeBits, syncCommitteeSignature);
        var optimisticUpdate = AltairLightClientOptimisticUpdate.CreateFrom(attestedHeader, syncAggregate, signatureSlot);
        var deserializedOptimisticUpdate = AltairLightClientOptimisticUpdate.Deserialize(AltairLightClientOptimisticUpdate.Serialize(optimisticUpdate));
        
        Assert.That(deserializedOptimisticUpdate.AttestedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])));
        Assert.That(deserializedOptimisticUpdate.AttestedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AttestedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AttestedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AttestedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["body_root"].Remove(0, 2)));
        Assert.That(syncCommitteeBits, Is.EqualTo(deserializedOptimisticUpdate.AltairSyncAggregate.SyncCommitteeBits));
        Assert.That(Convert.ToHexString(deserializedOptimisticUpdate.AltairSyncAggregate.SyncCommitteeSignature.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(syncCommitteeSignature.AsSpan()).ToLower()));
        Assert.That(deserializedOptimisticUpdate.SignatureSlot, Is.EqualTo(ulong.Parse((string)yamlData["signature_slot"])));
    }
    
    [Test]
    [TestCase("mainnet/altair/ssz_static/LightClientFinalityUpdate/ssz_random/case_0/value.yaml")]
    public void LightClientFinalityUpdate_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
    
        var yamlData = _yamlData;
        var attestedSlot = ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"]);
        var attestedProposerIndex = ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"]);
        var attestedParentRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["parent_root"]);
        var attestedStateRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["state_root"]);
        var attestedBodyRoot = TestUtility.HexToByteArray((string)yamlData["attested_header"]["beacon"]["body_root"]);
        var finalizedSlot = ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"]);
        var finalizedProposerIndex = ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"]);
        var finalizedParentRoot = TestUtility.HexToByteArray((string)yamlData["finalized_header"]["beacon"]["parent_root"]);
        var finalizedStateRoot = TestUtility.HexToByteArray((string)yamlData["finalized_header"]["beacon"]["state_root"]);
        var finalizedBodyRoot = TestUtility.HexToByteArray((string)yamlData["finalized_header"]["beacon"]["body_root"]);
        var finalizedBranch = new byte[Constants.FinalityBranchDepth][];
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            finalizedBranch[i] = TestUtility.HexToByteArray((string)yamlData["finality_branch"][i]);
        }
        
        var syncCommitteeBitsArray = new BitArray(TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_bits"]));
        var syncCommitteeBits = syncCommitteeBitsArray.Cast<bool>().ToList();
        var syncCommitteeSignature = TestUtility.HexToByteArray((string)yamlData["sync_aggregate"]["sync_committee_signature"]);
        var signatureSlot = ulong.Parse((string)yamlData["signature_slot"]);
        var attestedHeader = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(attestedSlot, attestedProposerIndex, attestedParentRoot, attestedStateRoot, attestedBodyRoot));
        var finalizedHeader = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(finalizedSlot, finalizedProposerIndex, finalizedParentRoot, finalizedStateRoot, finalizedBodyRoot));
        var syncAggregate = AltairSyncAggregate.CreateFrom(syncCommitteeBits, syncCommitteeSignature);
        var lightClientFinalityUpdate = AltairLightClientFinalityUpdate.CreateFrom(attestedHeader, finalizedHeader, finalizedBranch, syncAggregate, signatureSlot);
        var deserializedLightClientFinalityUpdate = AltairLightClientFinalityUpdate.Deserialize(AltairLightClientFinalityUpdate.Serialize(lightClientFinalityUpdate));
        
        Assert.That(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["slot"])));
        Assert.That(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["attested_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AttestedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["attested_header"]["beacon"]["body_root"].Remove(0, 2)));
        Assert.That(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["slot"])));
        Assert.That(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["finalized_header"]["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalizedHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["finalized_header"]["beacon"]["body_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.FinalityBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.FinalityBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(finalizedBranch[i].AsSpan()).ToLower()));
        }
        
        Assert.That(syncCommitteeBits, Is.EqualTo(deserializedLightClientFinalityUpdate.AltairSyncAggregate.SyncCommitteeBits));
        Assert.That(Convert.ToHexString(deserializedLightClientFinalityUpdate.AltairSyncAggregate.SyncCommitteeSignature.AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(syncCommitteeSignature.AsSpan()).ToLower()));
        Assert.That(deserializedLightClientFinalityUpdate.SignatureSlot, Is.EqualTo(ulong.Parse((string)yamlData["signature_slot"])));
    }
    
    [Test]
    [TestCase("mainnet/bellatrix/ssz_static/ExecutionPayloadHeader/ssz_random/case_0/value.yaml")]
    public void ExecutionPayloadHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
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
        var deserializedHeader = BellatrixExecutionPayloadHeader.Deserialize(BellatrixExecutionPayloadHeader.Serialize(header));
        var bytes = BellatrixExecutionPayloadHeader.Serialize(header);
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
    
    [Test]
    [TestCase("mainnet/capella/ssz_static/ExecutionPayloadHeader/ssz_random/case_0/value.yaml")]
    public void CapellaExecutionPayloadHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
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
        var header = CapellaExecutionPayloadHeader.CreateFrom(parentHash, feeRecipient, stateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot); 
        var deserializedHeader =  CapellaExecutionPayloadHeader.Deserialize(CapellaExecutionPayloadHeader.Serialize(header));
        
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
    }
    
    [Test]
    [TestCase("mainnet/capella/ssz_static/LightClientHeader/ssz_random/case_0/value.yaml")]
    public void CapellaLightClientHeader_Serializer_ShouldSerializeAndDeserializeCorrectly(string filePath)
    {
        LoadYamlFixture(new YamlFixtureAttribute(Path.Combine(_dataFolderPath, filePath)));
        
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
        var execution = CapellaExecutionPayloadHeader.CreateFrom(parentHash, feeRecipient, executionStateRoot, receiptsRoot, logsBloom, prevRandao, blockNumber, gasLimit, gasUsed, timestamp, extraData, baseFeePerGas, blockHash, transactionsRoot, withdrawalsRoot);
        var executionBranch = new byte[Constants.ExecutionBranchDepth][];
        
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            executionBranch[i] = TestUtility.HexToByteArray((string)yamlData["execution_branch"][i]);
        }
        
        var lightClientHeader = CapellaLightClientHeader.CreateFrom(beacon, execution, executionBranch);
        var deserializedLightClientHeader = CapellaLightClientHeader.Deserialize(CapellaLightClientHeader.Serialize(lightClientHeader));
        
        Assert.That((ulong)deserializedLightClientHeader.Beacon.Slot, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["slot"])));
        Assert.That((ulong)deserializedLightClientHeader.Beacon.ProposerIndex, Is.EqualTo(ulong.Parse((string)yamlData["beacon"]["proposer_index"])));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Beacon.ParentRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["parent_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Beacon.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.Beacon.BodyRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["beacon"]["body_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.ParentHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["parent_hash"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.FeeRecipientAddress.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["fee_recipient"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.StateRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["state_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.ReceiptsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["receipts_root"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.LogsBloom).ToLower(), Is.EqualTo((string)yamlData["execution"]["logs_bloom"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.PrevRandoa.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["prev_randao"].Remove(0, 2)));
        Assert.That(deserializedLightClientHeader.BellatrixExecution.BlockNumber, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["block_number"])));
        Assert.That(deserializedLightClientHeader.BellatrixExecution.GasLimit, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["gas_limit"])));
        Assert.That(deserializedLightClientHeader.BellatrixExecution.GasUsed, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["gas_used"])));
        Assert.That(deserializedLightClientHeader.BellatrixExecution.Timestamp, Is.EqualTo(ulong.Parse((string)yamlData["execution"]["timestamp"])));
        Assert.That(deserializedLightClientHeader.BellatrixExecution.ExtraData, Is.EqualTo(execution.ExtraData));
        Assert.That(deserializedLightClientHeader.BellatrixExecution.BaseFeePerGas, Is.EqualTo(BigInteger.Parse((string)yamlData["execution"]["base_fee_per_gas"])));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.BlockHash.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["block_hash"].Remove(0, 2)));
        Assert.That(Convert.ToHexString(deserializedLightClientHeader.BellatrixExecution.TransactionsRoot.AsSpan()).ToLower(), Is.EqualTo((string)yamlData["execution"]["transactions_root"].Remove(0, 2)));
        
        for (var i = 0; i < Constants.ExecutionBranchDepth; i++)
        {
            Assert.That(Convert.ToHexString(deserializedLightClientHeader.ExecutionBranch[i].AsSpan()).ToLower(), Is.EqualTo(Convert.ToHexString(executionBranch[i].AsSpan()).ToLower()));
        }
    }
}