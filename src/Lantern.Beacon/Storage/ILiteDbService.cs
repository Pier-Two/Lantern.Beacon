using System.Linq.Expressions;
using LiteDB;

namespace Lantern.Beacon.Storage;

public interface ILiteDbService
{
    void Init();
    
    void Store<T>(string collectionName, T item);

    void StoreOrUpdate<T>(string collectionName, T item) where T : class, IEquatable<T>, new();
    
    T? Fetch<T>(string collectionName);

    T? FetchByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate);
    
    void Dispose();
}