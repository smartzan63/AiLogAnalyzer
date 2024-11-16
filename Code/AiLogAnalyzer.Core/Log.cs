namespace AiLogAnalyzer.Core;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Serilog;

[ExcludeFromCodeCoverage]
public static class Log
{
    private const string OutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message}{NewLine}{Exception}";
    private static bool _isConfigured;

    static Log()
    {
        if (!_isConfigured)
        {
            ConfigureDefaultLogger();
        }
    }
    
    public static void SetLogger(ILogger logger)
    {
        Serilog.Log.Logger = logger;
        _isConfigured = true;
    }

    public static void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        LogWithLevel(Serilog.Log.Logger.Debug, message, filePath, lineNumber);
    }

    public static void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        LogWithLevel(Serilog.Log.Logger.Information, message, filePath, lineNumber);
    }

    public static void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        LogWithLevel(Serilog.Log.Logger.Warning, message, filePath, lineNumber);
    }

    public static void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
    {
        LogWithLevel(Serilog.Log.Logger.Error, message, filePath, lineNumber);
    }
    
    private static void ConfigureDefaultLogger()
    {
        if (_isConfigured) return;

        var defaultLogLevel = "Debug";

        var logEventLevel = Enum.TryParse(defaultLogLevel, true, out Serilog.Events.LogEventLevel parsedLogLevel)
            ? parsedLogLevel
            : Serilog.Events.LogEventLevel.Error;

        var loggerConfig = new LoggerConfiguration().MinimumLevel.Is(logEventLevel);

        var logDirectoryPath = "C:/logs/AiLogAnalyzer";
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        var logFilePath = $"{logDirectoryPath}/{timestamp}.log";

        loggerConfig = loggerConfig
            .WriteTo.Async(a => a.Console(
                outputTemplate: OutputTemplate,
                restrictedToMinimumLevel: logEventLevel
            ))
            .WriteTo.Async(a => a.File(
                logFilePath,
                outputTemplate: OutputTemplate,
                restrictedToMinimumLevel: logEventLevel));

        Console.WriteLine($"Logging to file: {logFilePath}");

        Serilog.Log.Logger = loggerConfig.CreateLogger();
        _isConfigured = true;
    }

    private static void LogWithLevel(Action<string, object[]> logAction, string message, string filePath, int lineNumber)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            logAction($"[{fileName}:{lineNumber}] {message}", Array.Empty<object>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An exception occurred while logging: {ex}");
        }
    }
}