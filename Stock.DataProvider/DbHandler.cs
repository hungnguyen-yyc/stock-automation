using Microsoft.Data.Sqlite;
using Stock.DataProvider;
using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Data
{
    public class DbHandler
    {
        private const string _dbPath = @"C:\Users\hnguyen\Documents\stock-back-test\stock-historical-data.db";

        public async Task FillDbWithTickerPrice(string ticker, Timeframe timeframe, DateTime from)
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
            }

            using var conn = new SqliteConnection($"Data Source={_dbPath}");
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
            var lastDate = result == null ? from : Convert.ToDateTime(result);

            var prices = await collector.CollectData(ticker, timeframe, lastDate, DateTime.Now);
            if (prices == null || prices.Count == 0)
            {
                return;
            }

            InsertHistoricalPriceToTable(conn, tickerId, table, prices.ToArray());

            conn.Close();
        }

        private void InsertHistoricalPriceToTable(SqliteConnection conn, int tickerId, string table, IReadOnlyCollection<Price> dailyPrices)
        {
            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var price in dailyPrices)
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = $"INSERT OR IGNORE INTO {table} (ticker_id, Date, Open, High, Low, Close, Volume) VALUES (@TickerId, @Date, @Open, @High, @Low, @Close, @Volume)";

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
