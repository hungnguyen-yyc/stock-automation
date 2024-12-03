using Serilog;

namespace Stock.Trading;

public class LogInitializer
{
    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private const string CryptoTradingPathName = "Crypto.Trading/logs";
    private static readonly object Lock = new(); // Ensure thread safety
    private static bool _isInitialized = false;
    
    #if DEBUG
    private const string LogFilename = "log-debug-.txt";
    #else
    private const string LogFilename = "log-.txt";
    #endif
    
    public static ILogger GetLogger()
    {
        lock (Lock)
        {
            if (_isInitialized)
            {
                return Log.Logger;
            }

            var logDirectory = Path.Combine(LocalAppData, CryptoTradingPathName);
            Directory.CreateDirectory(logDirectory); // Ensure directory exists

            var logFullPath = Path.Combine(logDirectory, LogFilename);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // Set minimum log level
                .WriteTo.File(logFullPath, rollingInterval: RollingInterval.Day, fileSizeLimitBytes: 50000000) // Write logs to a file
                .CreateLogger();

            _isInitialized = true; // Mark as initialized
        }

        return Log.Logger;
    }
}