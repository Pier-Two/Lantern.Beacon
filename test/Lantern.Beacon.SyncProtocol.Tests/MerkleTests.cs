using Cortex.Containers;
using Lantern.Beacon.SyncProtocol.Types.Phase0;
using NUnit.Framework;

namespace Lantern.Beacon.SyncProtocol.Tests;

[TestFixture]
public class MerkleTests : YamlFixtureBase
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
    public void Merkleize(string filePath)
    {
        
    }
    
}