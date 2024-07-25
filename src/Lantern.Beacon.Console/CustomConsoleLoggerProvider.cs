using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Console;

public class CustomConsoleLoggerProvider : ILoggerProvider
{
    private readonly CustomConsoleLogger.CustomConsoleLoggerConfiguration _config;
    private readonly Func<CustomConsoleLogger.CustomConsoleLoggerConfiguration, bool> _filter;

    public CustomConsoleLoggerProvider(Func<CustomConsoleLogger.CustomConsoleLoggerConfiguration, bool> filter, CustomConsoleLogger.CustomConsoleLoggerConfiguration config)
    {
        _filter = filter;
        _config = config;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CustomConsoleLogger(categoryName, _filter, _config);
    }

    public void Dispose()
    {
    }
}