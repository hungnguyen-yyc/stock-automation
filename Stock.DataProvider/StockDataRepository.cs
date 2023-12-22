using Microsoft.Data.Sqlite;
using Stock.DataProvider;
using Stock.Shared.Models;
using System.Diagnostics;

namespace Stock.Data
{
    public class StockDataRepository
    {
        private const string _dbPath = @"C:\Users\hnguyen\Documents\stock-back-test\stock-historical-data.db";

        // log delegate
        public event LogEventHander LogCreated;

        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
            LogCreated?.Invoke(new EventArgs.LogEventArg(message));
        }

        public async Task FillLatestDataForTheDay(string ticker, Timeframe timeframe, DateTime from, DateTime to)
        {
            // delete today's data in case it's not complete in data
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            try
            {
                var table = string.Empty;
                switch (timeframe)
                {
                    case Timeframe.Daily:
                        table = "daily_price";
                        break;
                    case Timeframe.Hour1:
                        table = "one_hour_price";
                        break;
                    case Timeframe.Minute15:
                        table = "fifteen_minute_price";
                        break;
                    case Timeframe.Minute30:
                        table = "thirty_minute_price";
                        break;
                    default:
                        throw new Exception("Timeframe not supported");
                }

                var cmd = conn.CreateCommand();
                cmd.CommandText = $"DELETE FROM {table} WHERE ticker_id = (SELECT ticker_id FROM ticker WHERE name = '{ticker}') AND Date >= '{from:yyyy-MM-dd} 00:00:00' AND Date <= '{to:yyyy-MM-dd HH:mm:ss}'";
                cmd.ExecuteNonQuery();

                await FillDbWithTickerPrice(ticker, timeframe, from);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task<IReadOnlyCollection<Price>> GetStockData(string ticker, Timeframe timeframe, DateTime from, DateTime to)
        {
            var list = new List<Price>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            var table = string.Empty;
            switch (timeframe)
            {
                case Timeframe.Daily:
                    table = "daily_price";
                    break;
                case Timeframe.Hour1:
                    table = "one_hour_price";
                    break;
                case Timeframe.Minute15:
                    table = "fifteen_minute_price";
                    break;
                case Timeframe.Minute30:
                    table = "thirty_minute_price";
                    break;
                default:
                    throw new Exception("Timeframe not supported");
            }

            var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT ticker_id FROM ticker WHERE name = '{ticker}'";
            var result = cmd.ExecuteScalar();
            var tickerId = Convert.ToInt32(result);

            if (Convert.ToInt32(result) == 0)
            {
                throw new Exception($"Ticker {ticker} not found in database");
            }

            try
            {
                cmd.CommandText = $"SELECT * FROM {table} WHERE ticker_id = {tickerId} AND Date >= '{from:yyyy-MM-dd HH:mm:ss}' AND Date <= '{to:yyyy-MM-dd HH:mm:ss}' ORDER BY Date ASC;";
                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var price = new Price
                    {
                        Date = Convert.ToDateTime(reader["date"]),
                        Open = Convert.ToDecimal(reader["open"]),
                        Close = Convert.ToDecimal(reader["close"]),
                        High = Convert.ToDecimal(reader["high"]),
                        Low = Convert.ToDecimal(reader["low"]),
                        Volume = Convert.ToInt64(reader["volume"])
                    };

                    list.Add(price);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            finally
            {
                Debug.WriteLine($"Finished getting data for ticker {ticker} and timeframe {timeframe}");
                conn.Close();
            }

            return list;
        }

        public async Task InsertAlerts(Alert alert)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {

            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when inserting alert {alert}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task FillDbWithTickerPrice(string ticker, Timeframe timeframe, DateTime from)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                var collector = new FmpStockDataProvider();
                var table = string.Empty;

                switch (timeframe)
                {
                    case Timeframe.Daily:
                        table = "daily_price";
                        break;
                    case Timeframe.Hour1:
                        table = "one_hour_price";
                        break;
                    case Timeframe.Minute15:
                        table = "fifteen_minute_price";
                        break;
                    case Timeframe.Minute30:
                        table = "thirty_minute_price";
                        break;
                    default:
                        throw new Exception("Timeframe not supported");
                }
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT ticker_id FROM ticker WHERE name = '{ticker}'";
                var result = cmd.ExecuteScalar();
                var tickerId = Convert.ToInt32(result);

                if (Convert.ToInt32(result) == 0)
                {
                    cmd.CommandText = "INSERT INTO Ticker (Name) VALUES (@Name); SELECT MAX(ticker_id) FROM Ticker;";
                    cmd.Parameters.AddWithValue("@Name", ticker);
                    tickerId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT MAX(Date) FROM {table} WHERE ticker_id = {tickerId}";
                result = cmd.ExecuteScalar();
                var lastDate = from;
                if (result != null && result != DBNull.Value)
                {
                    lastDate = Convert.ToDateTime(result);
                }

                Log($"Collecting data for ticker {ticker} at timeframe {timeframe}");
                var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var prices = await collector.CollectData(ticker, timeframe, lastDate, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone));
                if (prices == null || prices.Count == 0)
                {
                    return;
                }

                Log($"Inserting data for ticker {ticker} at timeframe {timeframe}");
                InsertHistoricalPriceToTable(conn, tickerId, table, prices.ToArray());
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when filling database with ticker {ticker} and timeframe {timeframe}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        private void InsertHistoricalPriceToTable(SqliteConnection conn, int tickerId, string table, IReadOnlyCollection<Price> dailyPrices)
        {
            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var price in dailyPrices)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = $"INSERT OR REPLACE INTO {table} (ticker_id, Date, Open, High, Low, Close, Volume) VALUES (@TickerId, @Date, @Open, @High, @Low, @Close, @Volume)";

                    cmd.Parameters.AddWithValue("@TickerId", tickerId);
                    cmd.Parameters.AddWithValue("@Date", price.Date);
                    cmd.Parameters.AddWithValue("@Open", price.Open);
                    cmd.Parameters.AddWithValue("@High", price.High);
                    cmd.Parameters.AddWithValue("@Low", price.Low);
                    cmd.Parameters.AddWithValue("@Close", price.Close);
                    cmd.Parameters.AddWithValue("@Volume", price.Volume);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
