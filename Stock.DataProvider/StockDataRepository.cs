using IBApi;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Stock.DataProvider;
using Stock.Shared.Models;
using Stock.Shared.Models.IBKR.Messages;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Stock.Data
{
    public class StockDataRepository
    {
        private const string _dbPath = @"C:\Users\hngde\source\repos\hungnguyen-yyc\stock-automation-db\stock.db";

        // log delegate
        public event LogEventHander LogCreated;

        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
            LogCreated?.Invoke(new EventArgs.LogEventArg(message));
        }

        public async Task<string[]?> GetOptionPriceAsync(string optionDetailByBarChart)
        {
            try
            {
                using var httpClient = new HttpClient();
                var optionDetail = optionDetailByBarChart.Split('|');
                var url = "https://webapp-proxy.aws.barchart.com/v1/ondemand/getEquityOptionsHistory.json?symbol={0}%7C{1}%7C{2}&fields=volatility,theoretical,delta,gamma,theta,vega,rho";
                var formattedUrl = string.Format(url, optionDetail[0], optionDetail[1], optionDetail[2]);
                var response = await httpClient.GetAsync(formattedUrl);
                string[]? options = null;

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var optionPrice = OptionPriceResponse.FromJson(content);

                    if (optionPrice == null)
                    {
                        return null;
                    }
                    else
                    {
                        options = optionPrice.OptionPrice.Select(x => x.ToString()).ToArray();
                    }
                }

                return options;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return null;
            }
        }

        public async Task<string[]?> GetOptionPriceAsync(string ticker, DateTime expiredDate, decimal strike, char optionRight)
        {
            try
            {
                using var httpClient = new HttpClient();
                var expiredDateStr = expiredDate.ToString("yyyyMMdd");
                var strikeStr = strike.ToString("0.00");
                strikeStr += optionRight;
                return await GetOptionPriceAsync($"{ticker}|{expiredDateStr}|{strikeStr}");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return null;
            }
        }

        public async Task<Options?> GetOptionChainAsync(string ticker, DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = "https://instruments-excel.aws.barchart.com/instruments/{0}/options?expired=true&symbols=true&start={1}&end={2}";
                var formattedUrl = string.Format(url, ticker, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                var response = await httpClient.GetAsync(formattedUrl);
                Options? options = null;

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var optionChains = OptionChain.FromJson(content);

                    if (optionChains == null)
                    {
                        return null;
                    }
                    else
                    {
                        options = optionChains.Options;
                    }
                }

                return options;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return null;
            }
        }

        public async Task FillLatestDataForTheDay(string ticker, Timeframe timeframe, DateTime from, DateTime to, int retries = 5)
        {
            if (retries == 0)
            {
                return;
            }

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

                var easternTo = GetEasternTime(to);
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"DELETE FROM {table} WHERE ticker_id = (SELECT ticker_id FROM ticker WHERE name = '{ticker}') AND Date >= '{from:yyyy-MM-dd} 00:00:00' AND Date <= '{easternTo:yyyy-MM-dd HH:mm:ss}'";
                cmd.ExecuteNonQuery();

                await FillDbWithTickerPrice(ticker, timeframe, from);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                await FillLatestDataForTheDay(ticker, timeframe, from, to, retries - 1);
                await Task.Delay(TimeSpan.FromSeconds(15));
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
                var easternTo = GetEasternTime(to);
                cmd.CommandText = $"SELECT * FROM {table} WHERE ticker_id = {tickerId} AND Date >= '{from:yyyy-MM-dd HH:mm:ss}' AND Date <= '{easternTo:yyyy-MM-dd HH:mm:ss}' ORDER BY Date ASC;";
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

        public async Task SaveSwingPointOptionPosition(PositionMessage position)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {              
                conn.Open();

                var contractId = await GetContractId(position.Contract);

                var cmd = conn.CreateCommand();

                cmd.CommandText = "INSERT INTO swing_point_option_position (option_contract_id, quantity, ave_price, account_id, is_closed) " +
                    "VALUES (@OptionContractId, @Quantity, @AvgPrice, @AccountId, @IsClosed)";
            
                cmd.Parameters.AddWithValue("@OptionContractId", contractId);
                cmd.Parameters.AddWithValue("@Quantity", position.Position);
                cmd.Parameters.AddWithValue("@AvgPrice", position.AverageCost);
                cmd.Parameters.AddWithValue("@AccountId", position.Account);
                cmd.Parameters.AddWithValue("@IsClosed", 0);
            
                await cmd.ExecuteNonQueryAsync();

                Log($"Created swing point option order for ticker {position.Contract.Symbol} with strike {position.Contract.Strike}{position.Contract.Right} expired on {position.Contract.LastTradeDateOrContractMonth}");
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when creating swing point option order for ticker {position.Contract.Symbol}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task CloseSwingPointOptionPosition(SwingPointOptionPosition position)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE swing_point_option_position SET is_closed = 1 WHERE swing_point_order_id = @OrderId";
                cmd.Parameters.AddWithValue("@OrderId", position.Id);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when closing swing point option order", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task<IReadOnlyCollection<SwingPointOptionPosition>> GetSavedSwingPointOptionPositions()
        {
            var list = new List<SwingPointOptionPosition>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT spop.*, t.name FROM swing_point_option_position spop"
                + "JOIN ticker t ON t.ticker_id = spop.ticker_id";
            var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                var position = new SwingPointOptionPosition
                {
                    Id = Convert.ToInt32(reader["swing_point_order_id"]),
                    TickerId = Convert.ToInt32(reader["ticker_id"]),
                    Ticker = Convert.ToString(reader["name"]),
                    Quantity = Convert.ToInt32(reader["quantity"]),
                    Price = Convert.ToDecimal(reader["price"]),
                    AccountId = Convert.ToString(reader["account_id"]),
                    ExpiredOn = Convert.ToDateTime(reader["expired_on"]),
                    Strike = Convert.ToDecimal(reader["strike"]),
                    OptionRight = Convert.ToString(reader["option_right"]),
                    LevelHigh = Convert.ToDecimal(reader["level_high"]),
                    LevelLow = Convert.ToDecimal(reader["level_low"]),
                    IsClosed = Convert.ToBoolean(reader["is_closed"])
                };

                list.Add(position);
            }

            conn.Close();

            return list;
        }

        public async Task SaveContract(Contract contract)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var tickerId = await GetTickerId(contract.Symbol);

                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO option_contract (ticker_id, expired_on, strike, option_right) VALUES (@TickerId, @ExpiredOn, @Strike, @OptionRight)";
                cmd.Parameters.AddWithValue("@TickerId", tickerId);
                cmd.Parameters.AddWithValue("@ExpiredOn", contract.LastTradeDateOrContractMonth);
                cmd.Parameters.AddWithValue("@Strike", contract.Strike);
                cmd.Parameters.AddWithValue("@OptionRight", contract.Right);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when saving contract {JsonConvert.SerializeObject(contract)}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task<int> GetContractId(Contract contract)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var tickerId = await GetTickerId(contract.Symbol);

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM option_contract WHERE ticker_id = @TickerId AND expired_on = @ExpiredOn AND strike = @Strike AND option_right = @OptionRight";
                cmd.Parameters.AddWithValue("@TickerId", tickerId);
                cmd.Parameters.AddWithValue("@ExpiredOn", contract.LastTradeDateOrContractMonth);
                cmd.Parameters.AddWithValue("@Strike", contract.Strike);
                cmd.Parameters.AddWithValue("@OptionRight", contract.Right);
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    var contractId = Convert.ToInt32(reader["option_contract_id"]);
                    return contractId;
                }
                else
                {
                    await SaveContract(contract);
                    return await GetContractId(contract);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when getting contract {JsonConvert.SerializeObject(contract)}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task SaveOptionContractWithTarget(Contract contract, TopNBottomStrategyAlert alert)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var savedContractId = await GetContractId(contract);

                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO option_contract_high_low_target (option_contract_id, level_high, level_low, center) VALUES (@ContractId, @LevelHigh, @LevelLow, @Center)";
                cmd.Parameters.AddWithValue("@ContractId", savedContractId);
                cmd.Parameters.AddWithValue("@LevelHigh", alert.High);
                cmd.Parameters.AddWithValue("@LevelLow", alert.Low);
                cmd.Parameters.AddWithValue("@Center", alert.Center);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when saving contract {JsonConvert.SerializeObject(contract)}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task<decimal?> GetLowerTargetForContract(Contract contract)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var savedContractId = await GetContractId(contract);

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT level_low FROM option_contract_high_low_target WHERE option_contract_id = @ContractId";
                cmd.Parameters.AddWithValue("@ContractId", savedContractId);
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    var levelLow = Convert.ToDecimal(reader["level_low"]);
                    return levelLow;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when getting contract {JsonConvert.SerializeObject(contract)}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task<decimal?> GetHigherTargetForContract(Contract contract)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var savedContractId = await GetContractId(contract);

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT level_high FROM option_contract_high_low_target WHERE option_contract_id = @ContractId";
                cmd.Parameters.AddWithValue("@ContractId", savedContractId);
                var reader = await cmd.ExecuteReaderAsync();

                if (reader.Read())
                {
                    var levelHigh = Convert.ToDecimal(reader["level_high"]);
                    return levelHigh;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when getting contract {JsonConvert.SerializeObject(contract)}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        public async Task<int> GetTickerId(string ticker)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT ticker_id FROM ticker WHERE name = '{ticker}'";
                var result = await cmd.ExecuteScalarAsync();
                var tickerId = Convert.ToInt32(result);

                if (Convert.ToInt32(result) == 0)
                {
                    cmd.CommandText = "INSERT INTO Ticker (Name) VALUES (@Name); SELECT MAX(ticker_id) FROM Ticker;";
                    cmd.Parameters.AddWithValue("@Name", ticker);
                    tickerId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                return tickerId;
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when getting ticker id for ticker {ticker}", ex);
            }
            finally
            {
                conn.Close();
            }
        }

        // TODO: for temporary use only, to fill data for new ticker, hardcode table name and interval
        public async Task QuickFill(string ticker, Timeframe timeframe, DateTime deleteFrom)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            try
            {
                var deleteFromString = deleteFrom.ToString("yyyyMMdd");
                var tablename = "thirty_minute_price";
                var interval = 30;
                var toDate = DateTime.Now.AddDays(1).ToString("yyyyMMdd");

                switch (timeframe)
                {
                    case Timeframe.Hour1:
                        tablename = "one_hour_price";
                        interval = 60;
                        break;
                    case Timeframe.Minute15:
                        tablename = "fifteen_minute_price";
                        interval = 15;
                        break;
                    case Timeframe.Minute30:
                        tablename = "thirty_minute_price";
                        interval = 30;
                        break;
                    default:
                        throw new Exception("Timeframe not supported");
                }

                var url = "https://ds01.ddfplus.com/historical/queryminutes.ashx?symbol={0}&start={1}&end={2}&contractroll=combined&order=Descending&interval={3}&fromt=false&username=randacchub%40gmail.com&password=_placeholder_";
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(string.Format(url, ticker, deleteFromString, toDate, interval));
                if (response.IsSuccessStatusCode)
                {
                    var tickerId = await GetTickerId(ticker);

                    //delete existing ticker data
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = $"DELETE FROM {tablename} WHERE ticker_id = {tickerId} AND DATE >= '{deleteFrom:yyyy-MM-dd hh:mm:ss}'";
                    cmd.ExecuteNonQuery();

                    string content = await response.Content.ReadAsStringAsync();
                    var lines = content.Split('\n');
                    var list = new List<Price>();
                    foreach (var line in lines)
                    {
                        var price = new Price();
                        var values = line.Split(',');
                        if (values.Length < 7)
                        {
                            continue;
                        }

                        price.Date = Convert.ToDateTime(values[0]);
                        price.Open = Convert.ToDecimal(values[2]);
                        price.High = Convert.ToDecimal(values[3]);
                        price.Low = Convert.ToDecimal(values[4]);
                        price.Close = Convert.ToDecimal(values[5]);
                        price.Volume = Convert.ToInt64(values[6]);

                        if (!price.isValid)
                        {
                            throw new Exception($"Invalid price {JsonConvert.SerializeObject(price)}");
                        }

                        list.Add(price);
                    }

                    InsertHistoricalPriceToTable(conn, tickerId, tablename, list.ToArray());
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error when creating csv data for ticker {ticker}", ex);
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

                var tickerId = await GetTickerId(ticker);

                var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT MAX(Date) FROM {table} WHERE ticker_id = {tickerId}";

                var result = cmd.ExecuteScalar();
                var lastDate = from;
                if (result != null && result != DBNull.Value)
                {
                    lastDate = Convert.ToDateTime(result);
                }

                Log($"Collecting data for ticker {ticker} at timeframe {timeframe}");
                var eaternTime = GetEasternTime(DateTime.Now);
                var prices = await collector.CollectData(ticker, timeframe, lastDate, eaternTime);
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

        private DateTime GetEasternTime(DateTime time)
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTime(time, easternZone);
            return easternTime;
        }
    }
}
