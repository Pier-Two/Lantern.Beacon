using LiteDB;
using System.Linq.Expressions;
using Lantern.Beacon.Sync;
using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Storage;

public sealed class LiteDbService(BeaconClientOptions beaconClientOptions, SyncProtocolOptions syncProtocolOptions, ILoggerFactory loggerFactory) : ILiteDbService, IDisposable
{
    private readonly string _dbName = "lantern.db";
    private readonly string _baseDirectory = "lantern";
    private LiteDatabase? _liteDatabase;
    private readonly object _lock = new();
    private readonly ILogger<LiteDbService> _logger = loggerFactory.CreateLogger<LiteDbService>();

    public void Init()
    {
        lock (_lock)
        {
            if (_liteDatabase != null)
            {
                throw new InvalidOperationException("LiteDbService already initialized");
            }

            if(beaconClientOptions.DataDirectoryPath == null)
            {
                throw new ArgumentNullException(nameof(beaconClientOptions.DataDirectoryPath));
            }
            
            var directoryPath = Path.Combine(
                beaconClientOptions.DataDirectoryPath, 
                _baseDirectory,
                syncProtocolOptions.Network.ToString()
            );

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            var databaseFilePath = Path.Combine(directoryPath, _dbName);
            
            _liteDatabase = new LiteDatabase(databaseFilePath);
            _logger.LogInformation("Using data directory with path: {Path}", databaseFilePath);
        }
    }

    public void Store<T>(string collectionName, T item)
    {
        lock (_lock)
        {
            if (_liteDatabase == null)
            {
                throw new InvalidOperationException("LiteDbService not initialized");
            }

            var collection = _liteDatabase.GetCollection<T>(collectionName);
            collection.Insert(item);
        }
    }
    
    public void RemoveByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            if (_liteDatabase == null)
            {
                throw new InvalidOperationException("LiteDbService not initialized");
            }

            var collection = _liteDatabase.GetCollection<T>(collectionName);
            collection.DeleteMany(predicate);
        }
    }
    
    public void ReplaceByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate, T item)
    {
        lock (_lock)
        {
            if (_liteDatabase == null)
            {
                throw new InvalidOperationException("LiteDbService not initialized");
            }

            var collection = _liteDatabase.GetCollection<T>(collectionName);
            var existingItem = collection.FindOne(predicate);
            
            if (existingItem != null)
            {
                collection.Update(existingItem);
            }
            else
            {
                collection.Insert(item);
            }
        }
    }

    public void ReplaceAllWithItem<T>(string collectionName, T item) where T : class, IEquatable<T>, new()
    {
        lock (_lock)
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
    }

    public T? Fetch<T>(string collectionName)
    {
        lock (_lock)
        {
            if (_liteDatabase == null)
            {
                throw new InvalidOperationException("LiteDbService not initialized");
            }

            var collection = _liteDatabase.GetCollection<T>(collectionName);
            return collection.FindAll().FirstOrDefault();
        }
    }
    
    public IEnumerable<T?> FetchAll<T>(string collectionName)
    {
        lock (_lock)
        {
            if (_liteDatabase == null)
            {
                throw new InvalidOperationException("LiteDbService not initialized");
            }

            var collection = _liteDatabase.GetCollection<T>(collectionName);
            return collection.FindAll();
        }
    }

    public T? FetchByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            if (_liteDatabase == null)
            {
                throw new InvalidOperationException("LiteDbService not initialized");
            }

            var collection = _liteDatabase.GetCollection<T>(collectionName);
            return collection.FindOne(predicate);
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
    }
    
    private void Dispose(bool disposing)
    {
        lock (_lock)
        {
            if (disposing)
            {
                _liteDatabase?.Dispose();
            }
        }
    }
}