using LiteDB;

namespace Lantern.Beacon.Storage;

public class LiteDbService(BeaconClientOptions options) : IDisposable
{
    private readonly LiteDatabase _liteDatabase = new(options.DataDirectoryPath);
    
    public void Dispose()
    {
        _liteDatabase?.Dispose();
    }
}