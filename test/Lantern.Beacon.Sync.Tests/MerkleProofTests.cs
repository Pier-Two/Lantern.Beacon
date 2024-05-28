using NUnit.Framework;

namespace Lantern.Beacon.Sync.Tests;

[TestFixture]
public class MerkleProofTests : FileFixtureBase
{
    private string _dataFolderPath;
    
    [SetUp]
    public void Setup()
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(CapellaSimpleSerializeTests).Assembly.Location);
        var projectFolderPath = Directory.GetParent(assemblyFolder).Parent.Parent.FullName;
        _dataFolderPath = Path.Combine(projectFolderPath, "MockData");
    }
    
    [Test]
    [TestCase("mainnet/phase0/ssz_static/BeaconBlockHeader/ssz_random/case_0/value.yaml")]
    public void VerifyCurrentSyncCommitteeMerkleProof(string filePath)
    {
        
    }
    
    [Test]
    [TestCase("mainnet/phase0/ssz_static/BeaconBlockHeader/ssz_random/case_0/value.yaml")]
    public void VerifyFinalityMerkleProof(string filePath)
    {
        
    }
    
    [Test]
    [TestCase("mainnet/phase0/ssz_static/BeaconBlockHeader/ssz_random/case_0/value.yaml")]
    public void VerifyNextSyncCommitteeMerkleProof(string filePath)
    {
        
    }
    
}