using LiteDB;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Storage;

public sealed class LiteDbService(BeaconClientOptions options, ILoggerFactory loggerFactory) : ILiteDbService, IDisposable
{
    private LiteDatabase? _liteDatabase;
    private readonly ILogger<LiteDbService> _logger = loggerFactory.CreateLogger<LiteDbService>();

    public void Init()
    {
        if (_liteDatabase != null)
        {
            throw new InvalidOperationException("LiteDbService already initialized");
        }
        
        var directoryPath = Path.GetDirectoryName(options.DataDirectoryPath);
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        _liteDatabase = new LiteDatabase(options.DataDirectoryPath);
        _logger.LogInformation("Data directory initialized with path: {Path}", options.DataDirectoryPath);
    }
    
    public void Store<T>(string collectionName, T item)
    {
        if(_liteDatabase == null)
        {
            throw new InvalidOperationException("LiteDbService not initialized");
        }
        
        var collection = _liteDatabase.GetCollection<T>(collectionName);
        collection.Insert(item);
    }
    
    public void StoreOrUpdate<T>(string collectionName, T item) where T : class, IEquatable<T>, new()
    {
        if (_liteDatabase == null)
        {
            throw new InvalidOperationException("LiteDbService not initialized");
        }

        var collection = _liteDatabase.GetCollection<T>(collectionName);
        var existingItem = collection.FindAll().FirstOrDefault();

        if (existingItem != null)
        {
            collection.DeleteAll();
        }

        collection.Insert(item);
    }
    
    public T? Fetch<T>(string collectionName)
    {
        if (_liteDatabase == null)
        {
            throw new InvalidOperationException("LiteDbService not initialized");
        }

        var collection = _liteDatabase.GetCollection<T>(collectionName);
        return collection.FindAll().FirstOrDefault();
    }
    
    public T? FetchByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate)
    {
        if(_liteDatabase == null)
        {
            throw new InvalidOperationException("LiteDbService not initialized");
        }
        
        var collection = _liteDatabase.GetCollection<T>(collectionName);
        return collection.FindOne(predicate);
    }
    
    public bool UpdateItem<T>(string collectionName, T item) where T : new()
    {
        if (_liteDatabase == null)
        {
            throw new InvalidOperationException("LiteDbService not initialized");
        }

        var collection = _liteDatabase.GetCollection<T>(collectionName);
        return collection.Update(item);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _liteDatabase?.Dispose();
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
    }
}