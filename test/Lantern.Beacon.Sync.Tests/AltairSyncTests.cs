using Lantern.Beacon.Sync.Helpers;
using Lantern.Beacon.Sync.Presets;
using Lantern.Beacon.Sync.Processors;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Capella;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using NUnit.Framework;
using Lantern.Discv5.WireProtocol.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using SszSharp;
using ILoggerFactory = Castle.Core.Logging.ILoggerFactory;

namespace Lantern.Beacon.Sync.Tests;

[TestFixture]
public class AltairSyncTests : FileFixtureBase
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
    [TestCase("minimal/altair/sync/pyspec_tests/advance_finality_without_sync_committee")]
    public void AdvanceFinalityWithoutSyncCommittee(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;
        
        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = AltairLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);
        
        _options.GenesisValidatorsRoot = genesisValidatorsRoot;
        _syncProtocol.InitialiseStoreFromAltairBootstrap(trustedBlockRoot, bootstrap);
        
        foreach (var step in steps)
        {
            if(step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));
                
                var updateData = _sszData;
                var update = AltairLightClientUpdate.Deserialize(updateData, _options.Preset);
                
                AltairProcessors.ProcessLightClientUpdate(_syncProtocol.AltairLightClientStore, update, currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());
            
                Assert.That(_syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(_syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
            }
        }
    }
    
    [Test]
    [TestCase("minimal/altair/sync/pyspec_tests/capella_store_with_legacy_data")]
    public void CapellaStoreWithLegacyData(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;
        
        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = AltairLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);
        
        _syncProtocol.InitialiseStoreFromCapellaBootstrap(trustedBlockRoot, CapellaLightClientBootstrap.CreateFromAltair(bootstrap));
        
        foreach (var step in steps)
        {
            if(step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));
                
                var updateData = _sszData;
                var update = AltairLightClientUpdate.Deserialize(updateData, _options.Preset);
                
                CapellaProcessors.ProcessLightClientUpdate(_syncProtocol.CapellaLightClientStore, CapellaLightClientUpdate.CreateFromAltair(update), currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());
            
                Assert.That(_syncProtocol.CapellaLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.CapellaLightClientStore.FinalizedHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(CapellaHelpers.GetLcExecutionRoot(_syncProtocol.CapellaLightClientStore.FinalizedHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["execution_root"])));
                Assert.That(_syncProtocol.CapellaLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.CapellaLightClientStore.OptimisticHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
                Assert.That(CapellaHelpers.GetLcExecutionRoot(_syncProtocol.CapellaLightClientStore.OptimisticHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["execution_root"])));
            }
        }
    }
    
    [Test]
    [TestCase("minimal/altair/sync/pyspec_tests/deneb_store_with_legacy_data")]
    public void DenebStoreWithLegacyData(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;
        
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;
        
        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = AltairLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);
        
        _syncProtocol.InitialiseStoreFromDenebBootstrap(trustedBlockRoot, DenebLightClientBootstrap.CreateFromCapella(CapellaLightClientBootstrap.CreateFromAltair(bootstrap)));
        
        foreach (var step in steps)
        {
            if(step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));
                
                var updateData = _sszData;
                var update = AltairLightClientUpdate.Deserialize(updateData, _options.Preset);
                
                DenebProcessors.ProcessLightClientUpdate(_syncProtocol.DenebLightClientStore, DenebLightClientUpdate.CreateFromCapella(CapellaLightClientUpdate.CreateFromAltair(update)), currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());
            
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.FinalizedHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.FinalizedHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["finalized_header"]["execution_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.DenebLightClientStore.OptimisticHeader.Beacon.Slot, Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
                Assert.That(DenebHelpers.GetLcExecutionRoot(_syncProtocol.DenebLightClientStore.OptimisticHeader, _options.Preset), Is.EqualTo(TestUtility.HexToByteArray((string)step["process_update"]["checks"]["optimistic_header"]["execution_root"])));
            }
        }
    }

    [Test]
    [TestCase("minimal/altair/sync/pyspec_tests/light_client_sync")]
    public void LightClientSync(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;

        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = AltairLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);

        _options.GenesisValidatorsRoot = genesisValidatorsRoot;
        _syncProtocol.InitialiseStoreFromAltairBootstrap(trustedBlockRoot, bootstrap);

        foreach (var step in steps)
        {
            if (step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));

                var updateData = _sszData;
                var update = AltairLightClientUpdate.Deserialize(updateData, _options.Preset);

                AltairProcessors.ProcessLightClientUpdate(_syncProtocol.AltairLightClientStore, update, currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());

                Assert.That(
                    _syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(
                    _syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
            }
            else if (step.ContainsKey("force_update"))
            {
                var currentSlot = ulong.Parse(step["force_update"]["current_slot"]);
              
                AltairProcessors.ProcessLightClientStoreForceUpdate(_syncProtocol.AltairLightClientStore, currentSlot, Mock.Of<ILogger<SyncProtocol>>());
                
                Assert.That(
                    _syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["force_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["force_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(
                    _syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["force_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["force_update"]["checks"]["optimistic_header"]["slot"])));
            }
        }
    }
    
    [Test]
    [TestCase("minimal/altair/sync/pyspec_tests/supply_sync_committee_from_past_update")]
    public void SupplySyncCommitteeFromPastUpdate(string folderPath)
    {
        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "config.yaml")));
        var config = _yamlData;
        Config.Config.AltairForkEpoch = uint.Parse((string)config["ALTAIR_FORK_EPOCH"]);

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "meta.yaml")));
        var meta = _yamlData;

        LoadYamlFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "steps.yaml")));
        var steps = _yamlData as List<dynamic>;

        LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, "bootstrap.ssz_snappy")));
        var bootstrapData = _sszData;
        var genesisValidatorsRoot = TestUtility.HexToByteArray((string)meta["genesis_validators_root"]);
        var trustedBlockRoot = TestUtility.HexToByteArray((string)meta["trusted_block_root"]);
        var bootstrap = AltairLightClientBootstrap.Deserialize(bootstrapData, _options.Preset);

        _options.GenesisValidatorsRoot = genesisValidatorsRoot;
        _syncProtocol.InitialiseStoreFromAltairBootstrap(trustedBlockRoot, bootstrap);

        foreach (var step in steps)
        {
            if (step.ContainsKey("process_update"))
            {
                var currentSlot = ulong.Parse(step["process_update"]["current_slot"]);
                var updateFileName = step["process_update"]["update"] + ".ssz_snappy";
                LoadSszFixture(new FileFixtureAttribute(Path.Combine(_dataFolderPath, folderPath, updateFileName)));

                var updateData = _sszData;
                var update = AltairLightClientUpdate.Deserialize(updateData, _options.Preset);

                AltairProcessors.ProcessLightClientUpdate(_syncProtocol.AltairLightClientStore, update, currentSlot, _options, Mock.Of<ILogger<SyncProtocol>>());

                Assert.That(
                    _syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["finalized_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.FinalizedHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["finalized_header"]["slot"])));
                Assert.That(
                    _syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.GetHashTreeRoot(_options.Preset),
                    Is.EqualTo(TestUtility.HexToByteArray(
                        (string)step["process_update"]["checks"]["optimistic_header"]["beacon_root"])));
                Assert.That(_syncProtocol.AltairLightClientStore.OptimisticHeader.Beacon.Slot,
                    Is.EqualTo(uint.Parse((string)step["process_update"]["checks"]["optimistic_header"]["slot"])));
            }
        }
    }
}