using Microsoft.Extensions.Logging;

namespace Lantern.Beacon.Console;

public class CustomConsoleLogger(
    string name,
    Func<CustomConsoleLogger.CustomConsoleLoggerConfiguration, bool> filter,
    CustomConsoleLogger.CustomConsoleLoggerConfiguration config)
    : ILogger
{
    private readonly string _name = name ?? throw new ArgumentNullException(nameof(name));
    private readonly Func<CustomConsoleLoggerConfiguration, bool> _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    private readonly CustomConsoleLoggerConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => _filter(_config);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        if (_config.EventId == 0 || _config.EventId == eventId.Id)
        {
            var timestamp = DateTime.UtcNow.ToString(_config.TimestampPrefix); 
            var logLevelString = GetLogLevelString(logLevel);
            var logMessage = RemoveCategoryAndId(formatter(state, exception));
            System.Console.WriteLine($"{timestamp} [{logLevelString}] {logMessage}");
        }
    }

    private static string RemoveCategoryAndId(string message)
    {
        // Logic to remove the category and eventId from the message
        // Assuming the category will be of the pattern 'Category[EventId]: message'
        var index = message.IndexOf(']');
        
        if (index != -1 && message.Length > index + 1)
        {
            message = message[(index + 2)..];
        }
        
        return message;
    }
    
    private string GetLogLevelString(LogLevel logLevel)
    {
        // Convert log level to custom string representation
        return logLevel switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Info",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            LogLevel.None => "None",
            _ => "Unknown"
        };
    }
    
    public class CustomConsoleLoggerConfiguration
    {
        public int EventId { get; set; }
        public string TimestampPrefix { get; set; }
    }
}