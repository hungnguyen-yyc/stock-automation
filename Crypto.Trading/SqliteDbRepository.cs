using System.Data.SQLite;
using Serilog;
using Stock.Shared;
using Stock.Trading.Models;

namespace Stock.Trading;

internal class SqliteDbRepository
{
    private readonly SqliteDbInitializer _sqliteDbInitializer;
    private readonly ILogger _logger;

    public SqliteDbRepository(
        SqliteDbInitializer sqliteDbInitializer,
        ILogger logger)
    {
        _sqliteDbInitializer = sqliteDbInitializer;
        _logger = logger;
    }
    
    public void AddPosition(InHouseOpenPosition openPosition)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var insertCommand = connection.CreateCommand();
            var baseQuery = @"INSERT INTO Positions (Ticker, Quantity, AveragePrice, EntryDate";
            var valuesClause = "VALUES (@Ticker, @Quantity, @AveragePrice, @EntryDate";
    
            insertCommand.Parameters.AddWithValue("@Ticker", openPosition.Ticker);
            insertCommand.Parameters.AddWithValue("@Quantity", openPosition.Quantity);
            insertCommand.Parameters.AddWithValue("@AveragePrice", openPosition.AveragePrice);
            insertCommand.Parameters.AddWithValue("@EntryDate", openPosition.EntryTime.ToUnixTimeSeconds());

            if (openPosition.StopLossPrice.HasValue)
            {
                baseQuery += ", StopLoss";
                valuesClause += ", @StopLoss";
                insertCommand.Parameters.AddWithValue("@StopLoss", openPosition.StopLossPrice.Value);
            }

            if (openPosition.TakeProfitPrice.HasValue)
            {
                baseQuery += ", TakeProfit";
                valuesClause += ", @TakeProfit";
                insertCommand.Parameters.AddWithValue("@TakeProfit", openPosition.TakeProfitPrice.Value);
            }

            baseQuery += ")";
            valuesClause += ")";
            var insertQuery = $"{baseQuery} {valuesClause};";

            insertCommand.CommandText = insertQuery;
            insertCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error adding position to database");
        }
    }
    
    public void ClosePosition(InHouseClosedPosition position)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"UPDATE Positions SET ExitPrice = @ExitPrice, ExitDate = @ExitDate WHERE Ticker = @Ticker AND EntryDate = @EntryDate;";
        
            updateCommand.Parameters.AddWithValue("@Ticker", position.OpenPosition.Ticker);
            updateCommand.Parameters.AddWithValue("@ExitPrice", position.ExitPrice);
            updateCommand.Parameters.AddWithValue("@ExitDate", position.ExitTime.ToUnixTimeSeconds());
            updateCommand.Parameters.AddWithValue("@EntryDate", position.OpenPosition.EntryTime.ToUnixTimeSeconds());
        
            updateCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error closing position in database");
        }
    }
    
    
    
    public InHouseClosedPosition? GetClosedPosition(string ticker)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT Ticker, Quantity, AveragePrice, EntryDate, ExitPrice, ExitDate FROM Positions WHERE Ticker = @Ticker;";
        
            selectCommand.Parameters.AddWithValue("@Ticker", ticker);
        
            using var reader = selectCommand.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }
        
            var exitPrice = reader.GetDecimal(4);
            var exitTime = reader.GetInt64(5);
        
            return new InHouseClosedPosition(
                new InHouseOpenPosition(
                    ticker,
                    reader.GetDecimal(2),
                    reader.GetDecimal(1),
                    DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3))),
                exitPrice,
                DateTimeOffset.FromUnixTimeSeconds(exitTime));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting closed position from database");
            return null;
        }
    }
    
    public InHouseOpenPosition? GetOpenPosition(string ticker)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT Ticker, Quantity, AveragePrice, EntryDate, StopLoss, TakeProfit
                FROM Positions WHERE Ticker = @Ticker AND ExitPrice IS NULL;";
        
            selectCommand.Parameters.AddWithValue("@Ticker", ticker);
        
            using var reader = selectCommand.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }
        
            return new InHouseOpenPosition(
                ticker,
                reader.GetDecimal(2),
                reader.GetDecimal(1),
                DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)),
                reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting open position from database");
            return null;
        }
    }
    
    public decimal GetCryptoBalance(string crypto)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT Balance FROM Assets WHERE Ticker = @Ticker;";
            
            selectCommand.Parameters.AddWithValue("@Ticker", crypto);
        
            using var reader = selectCommand.ExecuteReader();
            if (!reader.Read())
            {
                return 0;
            }
        
            return reader.GetDecimal(0);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting balance from database");
            return 0;
        }
    }
    
    public void UpdateCryptoBalance(string crypto, decimal balance)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"UPDATE Assets SET Balance = @Balance WHERE Ticker = @Ticker;";
            
            updateCommand.Parameters.AddWithValue("@Ticker", crypto);
            updateCommand.Parameters.AddWithValue("@Balance", balance);
        
            updateCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating balance in database");
        }
    }
    
    public CryptoAssets GetCryptoAssets()
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT Ticker, Quantity, AveragePrice, EntryDate FROM Positions WHERE ExitPrice IS NULL;";
        
            using var reader = selectCommand.ExecuteReader();
            var assets = new CryptoAssets();
            while (reader.Read())
            {
                assets.Add(new CryptoAsset(
                    reader.GetString(0),
                    reader.GetDecimal(2),
                    reader.GetDecimal(1),
                    DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3))));
            }
        
            return assets;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting portfolio from database");
            return new CryptoAssets();
        }
    }
}