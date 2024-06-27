using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Processors;
using NUnit.Framework;
using Lantern.Beacon.Sync.Types.Deneb;
using Lantern.Discv5.WireProtocol.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using SszSharp;

namespace Lantern.Beacon.Sync.Tests;

public class DenebSyncTests : FileFixtureBase
{
    private string _dataFolderPath;
    private SyncProtocolOptions _options;
    private SyncProtocol _syncProtocol;
    
    [SetUp]
    public void Setup()
    {
        var assemblyFolder = Path.GetDirectoryName(typeof(AltairSyncTests).Assembly.Location);
        var projectFolderPath = Directory.GetParent(assemblyFolder).Parent.Parent.FullName;
        
        _dataFolderPath = Path.Combine(projectFolderPath, "MockData");
        _options = new SyncProtocolOptions();
        _options.Preset = SizePreset.MinimalPreset;
        _syncProtocol = new SyncProtocol(_options, LoggingOptions.Default);
        
        Config.Config.InitializeWithMinimal();
        Phase0Preset.InitializeWithMinimal();
        AltairPreset.InitializeWithMinimal();
    }
    
    [Test]
    [TestCase("minimal/deneb/sync/pyspec_tests/advance_finality_without_sync_committee")]
    public void AdvanceFinalityWithoutSyncCommittee(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);
        Config.Config.BellatrixForkEpoch = uint.Parse((string)config["BELLATRIX_FORK_EPOCH"]);
        Config.Config.CapellaForkEpoch = uint.Parse((string)config["CAPELLA_FORK_EPOCH"]);
        Config.Config.DenebForkEpoch = uint.Parse((string)config["DENEB_FORK_EPOCH"]);
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;
        
        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = DenebLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);
        
        _syncProtocol.InitialiseStoreFromDenebBootstrap(trustedBlockRoot, bootstrap);
        
        foreach (var step in steps)
        {
            if(step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));
                
                var updateData = _sszData;
                var update = DenebLightClientUpdate.Deserialize(updateData, _options.Preset);
                
                DenebProcessors.ProcessLightClientUpdate(_syncProtocol.DenebLightClientStore, update, currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());
            
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.FinalizedHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["execution_root"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.OptimisticHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["execution_root"])));
            }
        }
    }
    
    [Test]
    [TestCase("minimal/deneb/sync/pyspec_tests/light_client_sync")]
    public void LightClientSync(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);
        Config.Config.BellatrixForkEpoch = uint.Parse((string)config["BELLATRIX_FORK_EPOCH"]);
        Config.Config.CapellaForkEpoch = uint.Parse((string)config["CAPELLA_FORK_EPOCH"]);
        Config.Config.DenebForkEpoch = uint.Parse((string)config["DENEB_FORK_EPOCH"]);

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;

        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = DenebLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);

        _syncProtocol.InitialiseStoreFromDenebBootstrap(trustedBlockRoot, bootstrap);

        foreach (var step in steps)
        {
            if (step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                
                Console.WriteLine("\nProcessing update: " + updateFileName);
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));
                
                var updateData = _sszData;
                var update = DenebLightClientUpdate.Deserialize(updateData, _options.Preset);

                DenebProcessors.ProcessLightClientUpdate(_syncProtocol.DenebLightClientStore, update, currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());

                Assert.That(
                    _syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.FinalizedHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["execution_root"])));
                Assert.That(
                    _syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.OptimisticHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["execution_root"])));
            }
            else if (step.ContainsKey("force_update"))
            {
                var currentSlot = ulong.Parse(step["force_update"]["current_slot"]);
              
                DenebProcessors.ProcessLightClientStoreForceUpdate(_syncProtocol.DenebLightClientStore, currentSlot, Mock.Of<ILogger<SyncProtocol>>());

                Assert.That(
                    _syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["force_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["force_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.FinalizedHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["force_update"]["checks"]["finalized_header"]["execution_root"])));
                Assert.That(
                    _syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["force_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["force_update"]["checks"]["optimistic_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.OptimisticHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["force_update"]["checks"]["optimistic_header"]["execution_root"])));
            }
        }
    }
    
    [Test]
    [TestCase("minimal/deneb/sync/pyspec_tests/supply_sync_committee_from_past_update")]
    public void SupplySyncCommitteeFromPastUpdate(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);
        Config.Config.BellatrixForkEpoch = uint.Parse((string)config["BELLATRIX_FORK_EPOCH"]);
        Config.Config.CapellaForkEpoch = uint.Parse((string)config["CAPELLA_FORK_EPOCH"]);
        Config.Config.DenebForkEpoch = uint.Parse((string)config["DENEB_FORK_EPOCH"]);
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;

        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = DenebLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);

        _syncProtocol.InitialiseStoreFromDenebBootstrap(trustedBlockRoot, bootstrap);

        foreach (var step in steps)
        {
            if (step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));

                var updateData = _sszData;
                var update = DenebLightClientUpdate.Deserialize(updateData, _options.Preset);

                DenebProcessors.ProcessLightClientUpdate(_syncProtocol.DenebLightClientStore, update, currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());

                Assert.That(
                    _syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(
                    _syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
            }
        }
    }
}