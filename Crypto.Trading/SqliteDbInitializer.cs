using System.Data.SQLite;

namespace Stock.Trading;

internal class SqliteDbInitializer
{
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private const string CryptoTradingPathName = "Crypto.Trading";
    
    #if DEBUG
    private string TradingDbName = $"crypto-trading-debug-{DateTime.Now:yyyyMMddhhmmss}.db";
    #else
    private string TradingDbName = "crypto-trading.db";
    #endif
    
    public SqliteDbInitializer()
    {
        var path = Path.Combine(_localAppData, CryptoTradingPathName);
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        var dbPath = Path.Combine(path, TradingDbName);
        
        if (!File.Exists(dbPath))
        {
            SQLiteConnection.CreateFile(dbPath);
            using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            connection.Open();
            
            var createPositionsTableCommand = connection.CreateCommand();
            createPositionsTableCommand.CommandText = @"CREATE TABLE Positions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Ticker TEXT NOT NULL,
                Quantity REAL NOT NULL,
                AveragePrice REAL NOT NULL,
                EntryDate INTEGER NOT NULL,
                ExitPrice REAL,
                ExitDate INTEGER,
                StopLoss REAL,
                TakeProfit REAL
            );";
            createPositionsTableCommand.ExecuteNonQuery();
            
            var createBinanceOrderWithPivotPoints = connection.CreateCommand();
            createBinanceOrderWithPivotPoints.CommandText = @"CREATE TABLE BinanceOrdersWithStopLoss (
                BinanceOrderId INTEGER PRIMARY KEY,
                PivotPointJson TEXT,
            );";
            createBinanceOrderWithPivotPoints.ExecuteNonQuery();
            
            var createAssetsTableCommand = connection.CreateCommand();
            createAssetsTableCommand.CommandText = @"CREATE TABLE Assets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Ticker TEXT NOT NULL,
                Balance REAL NOT NULL
            );";
            createAssetsTableCommand.ExecuteNonQuery();
           
#if DEBUG
            // start assets with 100 each
            var insertAssetsCommand = connection.CreateCommand();
            insertAssetsCommand.CommandText = @"INSERT INTO Assets (Ticker, Balance) VALUES
                ('BTC', 100),
                ('ETH', 100),
                ('SHIB', 100),
                ('SOL', 100),
                ('DOGE', 100),
                ('SUI', 100);";
            insertAssetsCommand.ExecuteNonQuery();
#endif
        }
    }
    
    public string DbPath => Path.Combine(_localAppData, CryptoTradingPathName, TradingDbName);
}