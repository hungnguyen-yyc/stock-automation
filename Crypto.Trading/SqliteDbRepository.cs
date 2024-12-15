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
    
    public async Task AddPosition(InHouseOpenPosition openPosition)
    {
        try
        {
            await using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
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
            await insertCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error adding position to database");
        }
    }
    
    public async Task ClosePosition(InHouseClosedPosition position)
    {
        try
        {
            await using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"UPDATE Positions SET ExitPrice = @ExitPrice, ExitDate = @ExitDate WHERE Ticker = @Ticker AND EntryDate = @EntryDate;";
        
            updateCommand.Parameters.AddWithValue("@Ticker", position.OpenPosition.Ticker);
            updateCommand.Parameters.AddWithValue("@ExitPrice", position.ExitPrice);
            updateCommand.Parameters.AddWithValue("@ExitDate", position.ExitTime.ToUnixTimeSeconds());
            updateCommand.Parameters.AddWithValue("@EntryDate", position.OpenPosition.EntryTime.ToUnixTimeSeconds());
        
            await updateCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error closing position in database");
        }
    }
    
    public async Task<InHouseOpenPosition?> GetOpenPosition(string ticker)
    {
        try
        {
            await using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT Ticker, Quantity, AveragePrice, EntryDate, StopLoss, TakeProfit
                FROM Positions WHERE Ticker = @Ticker AND ExitPrice IS NULL;";
        
            selectCommand.Parameters.AddWithValue("@Ticker", ticker);

            await using var reader = await selectCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
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
    
    public async Task<decimal> GetCryptoBalance(string crypto)
    {
        try
        {
            await using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT Balance FROM Assets WHERE Ticker = @Ticker;";
            
            selectCommand.Parameters.AddWithValue("@Ticker", crypto);

            await using var reader = await selectCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
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
    
    public async Task CreateOrUpdateCryptoBalance(string crypto, decimal balance)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            // can't do an upsert in SQLite because of incremental primary key, so we have to check if the record exists first
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT COUNT(*) FROM Assets WHERE Ticker = @Ticker;";
            selectCommand.Parameters.AddWithValue("@Ticker", crypto);
            
            var count = (long)selectCommand.ExecuteScalar();
            if (count == 0)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"INSERT INTO Assets (Ticker, Balance) VALUES (@Ticker, @Balance);";
                insertCommand.Parameters.AddWithValue("@Ticker", crypto);
                insertCommand.Parameters.AddWithValue("@Balance", balance);
                await insertCommand.ExecuteNonQueryAsync();
            }
            else
            {
                await UpdateCryptoBalance(crypto, balance);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating or updating balance in database");
        }
    }
    
    public async Task CreateBinanceOrderWithPivotPoints(BinanceTradePivotPoint binanceTradePivotPoint)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"INSERT INTO BinanceOrdersWithStopLoss (BinanceOrderId, PivotPointJson) VALUES (@BinanceOrderId, @PivotPointJson);";
            insertCommand.Parameters.AddWithValue("@BinanceOrderId", binanceTradePivotPoint.BinanceTradeId);
            insertCommand.Parameters.AddWithValue("@PivotPointJson", binanceTradePivotPoint.TradePivotPoint?.Serialize());
        
            await insertCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating Binance order with pivot points in database");
        }
    }
    
    public async Task<long> GetNextBinanceOrderId()
    {
        try
        {
            await using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT MAX(BinanceOrderId) FROM BinanceOrdersWithStopLoss;";
        
            var result = await selectCommand.ExecuteScalarAsync();
            return result == DBNull.Value ? 1 : (long)(result ?? throw new InvalidOperationException("Error casting order id from db to long")) + 1;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting next Binance order ID from database");
            return 0;
        }
    }
    
    public async Task<BinanceTradePivotPoint?> GetBinanceOrderWithPivotPoints(long binanceOrderId)
    {
        try
        {
            await using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"SELECT PivotPointJson FROM BinanceOrdersWithStopLoss WHERE BinanceOrderId = @BinanceOrderId;";
            selectCommand.Parameters.AddWithValue("@BinanceOrderId", binanceOrderId);
        
            await using var reader = await selectCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }
        
            return BinanceTradePivotPoint.FromJson(binanceOrderId, reader.GetString(0));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting Binance order with pivot points from database");
            return null;
        }
    }
    
    private async Task UpdateCryptoBalance(string crypto, decimal balance)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_sqliteDbInitializer.DbPath};Version=3;");
            connection.Open();
        
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"UPDATE Assets SET Balance = @Balance WHERE Ticker = @Ticker;";
            
            updateCommand.Parameters.AddWithValue("@Ticker", crypto);
            updateCommand.Parameters.AddWithValue("@Balance", balance);
        
            await updateCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating balance in database");
        }
    }
}