using System.Linq.Expressions;
using LiteDB;

namespace Lantern.Beacon.Storage;

public interface ILiteDbService
{
    void Init();
    
    void Store<T>(string collectionName, T item);

    void ReplaceAllWithItem<T>(string collectionName, T item) where T : class, IEquatable<T>, new();
    
    T? Fetch<T>(string collectionName);
    
    IEnumerable<T?> FetchAll<T>(string collectionName);

    T? FetchByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate);

    void RemoveByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate);

    void ReplaceByPredicate<T>(string collectionName, Expression<Func<T, bool>> predicate, T item);
    
    void Dispose();
}