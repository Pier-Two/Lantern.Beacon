using Lantern.Beacon.Storage;
using Lantern.Beacon.Sync.Types.Ssz.Altair;
using Lantern.Beacon.Sync.Types.Ssz.Deneb;
using Lantern.Beacon.Sync.Types.Ssz.Phase0;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lantern.Beacon.Tests;

public class LiteDbServiceTests
{
    private LiteDbService _liteDbService;
    private BeaconClientOptions _beaconClientOptions;
    private LoggerFactory _loggerFactory;
    private string _testDirectoryPath;
    
    [SetUp]
    public void Setup()
    {
        _testDirectoryPath = Path.Combine(Path.GetTempPath(), "Lantern.Beacon.Tests", "LiteDbTests");

        if (Directory.Exists(_testDirectoryPath))
        {
            Directory.Delete(_testDirectoryPath, true);
        }

        Directory.CreateDirectory(_testDirectoryPath);

        _beaconClientOptions = new BeaconClientOptions
        {
            DataDirectoryPath = Path.Combine(_testDirectoryPath, "test.db")
        };

        _loggerFactory = new LoggerFactory();
        _liteDbService = new LiteDbService(_beaconClientOptions, _loggerFactory);
    }
    
    [Test]
    public void Init_ShouldInitializeCorrectly()
    {
        Assert.DoesNotThrow(() => _liteDbService.Init());
        Assert.That(_liteDbService, Is.Not.Null);
    }

    [Test]
    public void Init_ShouldThrowIfInitIsCalledTwice()
    {
        _liteDbService.Init();
        
        Assert.That(_liteDbService, Is.Not.Null);
        Assert.Throws<InvalidOperationException>(() => _liteDbService.Init());
    }
    
    [Test]
    public void Store_ShouldStoreItem()
    {
        var update = AltairLightClientHeader.CreateDefault();
        
        _liteDbService.Init();
        _liteDbService.Store(nameof(AltairLightClientHeader), update);
        
        var storedUpdate = _liteDbService.Fetch<AltairLightClientHeader>(nameof(AltairLightClientHeader));
        
        Assert.That(storedUpdate, Is.Not.Null);
        Assert.That(storedUpdate, Is.EqualTo(update));
    }
    
    [Test]
    public void Store_ShouldThrowIfNotInitialized()
    {
        var update = AltairLightClientHeader.CreateDefault();
        Assert.Throws<InvalidOperationException>(() => _liteDbService.Store(nameof(AltairLightClientHeader), update));
    }
    
    [Test]
    public void StoreOrUpdate_ShouldStoreAndUpdateItemCorrectly()
    {
        var update = AltairLightClientHeader.CreateDefault();
        var newUpdate = AltairLightClientHeader.CreateFrom(Phase0BeaconBlockHeader.CreateFrom(32332, 32, new byte[32], new byte[32], new byte[32]));
        
        _liteDbService.Init();
        _liteDbService.Store(nameof(AltairLightClientHeader), update);
        
        var storedUpdate = _liteDbService.Fetch<AltairLightClientHeader>(nameof(AltairLightClientHeader));
        
        Assert.That(storedUpdate, Is.Not.Null);
        Assert.That(storedUpdate, Is.EqualTo(update));
        
        _liteDbService.StoreOrUpdate(nameof(AltairLightClientHeader), newUpdate);
        
        storedUpdate = _liteDbService.Fetch<AltairLightClientHeader>(nameof(AltairLightClientHeader));
        
        Assert.That(storedUpdate, Is.Not.Null);
        Assert.That(storedUpdate, Is.EqualTo(newUpdate));
        Assert.That(storedUpdate, Is.Not.EqualTo(update));
        Assert.That(storedUpdate!.Beacon.Slot, Is.EqualTo(newUpdate.Beacon.Slot));
    }
    
    [Test]
    public void StoreOrUpdate_ShouldThrowIfNotInitialized()
    {
        var update = AltairLightClientHeader.CreateDefault();
        Assert.Throws<InvalidOperationException>(() => _liteDbService.StoreOrUpdate(nameof(AltairLightClientHeader), update));
    }
    
    [Test]
    public void Fetch_ShouldFetchItemCorrectly()
    {
        var update = AltairLightClientHeader.CreateDefault();
        
        _liteDbService.Init();
        _liteDbService.Store(nameof(AltairLightClientHeader), update);
        
        var storedUpdate = _liteDbService.Fetch<AltairLightClientHeader>(nameof(AltairLightClientHeader));
        
        Assert.That(storedUpdate, Is.Not.Null);
        Assert.That(storedUpdate, Is.EqualTo(update));
    }
    
    [Test]
    public void Fetch_ShouldThrowIfNotInitialized()
    {
        Assert.Throws<InvalidOperationException>(() => _liteDbService.Fetch<AltairLightClientHeader>(nameof(AltairLightClientHeader)));
    }
    
    [Test]
    public void FetchByPredicate_ShouldFetchItemCorrectly()
    {
        var update = AltairLightClientHeader.CreateDefault();
        
        _liteDbService.Init();
        _liteDbService.Store(nameof(AltairLightClientHeader), update);
        
        var storedUpdate = _liteDbService.FetchByPredicate<AltairLightClientHeader>(nameof(AltairLightClientHeader), x => x.Beacon.Slot == 0);
        
        Assert.That(storedUpdate, Is.Not.Null);
        Assert.That(storedUpdate, Is.EqualTo(update));
    }
    
    [Test]
    public void FetchByPredicate_ShouldThrowIfNotInitialized()
    {
        Assert.Throws<InvalidOperationException>(() => _liteDbService.FetchByPredicate<AltairLightClientHeader>(nameof(AltairLightClientHeader), x => x.Beacon.Slot == 0));
    }
    
    [Test]
    public void Dispose_ShouldDisposeCorrectly()
    {
        _liteDbService.Init();
        _liteDbService.Dispose();
        
        Assert.That(_liteDbService, Is.Not.Null);
    }
    
    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_testDirectoryPath))
        {
            Directory.Delete(_testDirectoryPath, true);
        }
    }
}