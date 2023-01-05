using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Diagnostics;
using System.Text;

namespace StockSignalScanner
{
    public partial class Program
    {
        static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static async Task Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                var northAmericaStocks = await GetStocksFromUSCANExchanges(10000000, API_KEY); // update to get correct exchanges

                var random = new Random();
                var nowTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
                var folderPath = @"C:\Users\hnguyen\Documents\stock-scan-logs";
                var scanFolderPath = Path.Combine(folderPath, nowDate);
                var allCrosses5 = new List<string>();
                var allCrosses14 = new List<string>();
                var overboughtOrOversoldFollowedByMACDCrossLast5Days = new List<string>();
                var mix = new List<string>();
                try
                {
                    if (Directory.Exists(scanFolderPath))
                    {
                        Directory.Delete(scanFolderPath, true);
                    }
                    CreateScanDirectories(folderPath, scanFolderPath);
                }
                catch (Exception)
                {

                }

                var batches = northAmericaStocks.OrderBy(s => s.Symbol).Chunk(290).ToArray();
                for (int si = 0; si < batches.Count(); si++)
                {
                    var stocks = batches[si];
                    foreach (var stock in stocks)
                    {
                        try
                        {
                            Console.WriteLine($"Start processing for {stock.Name} - {stock.Symbol}");
                            var data = await RunScan(stock.Symbol, API_KEY);
                            if (data != null)
                            {
                                if (data.HasOverboughtOrOversoldFollowedByMACDCrossLastNDays())
                                {
                                    overboughtOrOversoldFollowedByMACDCrossLast5Days.Add(data.GetTickerStatusLast5Days());
                                }
                                if (data.AllCrossesAbove5 || data.AllCrossesBelow5)
                                {
                                    allCrosses5.Add(data.GetTickerStatusLast5Days());
                                }
                                else if (data.AllCrossesAbove14 || data.AllCrossesBelow14)
                                {
                                    allCrosses14.Add(data.GetTickerStatusLast14Days());
                                }
                                else
                                {
                                    mix.Add(data.GetTickerStatusLast5Days());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, $"error-{stock.Symbol}-{nowTime}.txt"), true))
                            {
                                outputFile.WriteLine(ex.Message);
                                outputFile.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "all-crosses-last-5-days.txt"), true))
                {
                    foreach (var item in allCrosses5)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "all-crosses-last-14-days.txt"), true))
                {
                    foreach (var item in allCrosses14)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "overbought-or-oversold-with-macd-cross.txt"), true))
                {
                    foreach (var item in overboughtOrOversoldFollowedByMACDCrossLast5Days)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "mix.txt"), true))
                {
                    foreach (var item in mix)
                    {
                        outputFile.WriteLine(item);
                    }
                }
            }
        }

        #region will print rsi macd stochastic data for each stocks
        /*
         public static async Task Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                var northAmericaStocks = await GetStocksFromUSCANExchanges(API_KEY); // update to get correct exchanges

                var random = new Random();
                var randomNumber = random.Next(1, northAmericaStocks.Count() - 1);
                var nowTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
                var folderPath = @"C:\Users\hnguyen\Documents\stock-scan-logs";
                var scanFolderPath = Path.Combine(folderPath, nowDate);
                var last14DaysCrossFolder = Path.Combine(scanFolderPath, "crosses-in-last-14");
                var last5DaysCrossFolder = Path.Combine(scanFolderPath, "crosses-in-last-5");
                try
                {
                    Directory.Delete(scanFolderPath, true);
                    CreateScanDirectories(folderPath, scanFolderPath, last14DaysCrossFolder, last5DaysCrossFolder);
                }catch(Exception)
                {

                }

                //var stocks = northAmericaStocks;
                var batches = northAmericaStocks.Chunk(290);
                foreach (var stocks in batches)
                {
                    foreach (var stock in stocks)
                    {
                        try
                        {
                            var tickerActionString = "";
                            var fileName = Path.Combine(scanFolderPath, $"{stock.Symbol}-{nowTime}.csv");
                            Console.WriteLine($"Getting data for {stock.Name} - {stock.Symbol}");
                            var allIndicatorCrossed5 = false;
                            var allIndicatorCrossed14 = false;
                            using (StreamWriter outputFile = new StreamWriter(fileName, true))
                            {
                                outputFile.WriteLine("Time,Ticker,Exchange,PriceClose,Volume,RSI,StochasticK,StochasticD,MACD,MACDSignal,RSICheck,StochCheck,MACDCheck,RSICrossDirectionLast14Days,StochCrossDirectionLast14Days,MACDCrossDirectionLast14Days");
                                var data = await RunScan(stock.Symbol, stock.ExchangeShortName, API_KEY);
                                if (data != null)
                                {
                                    var reverse = data.OrderByDescending(d => d.Date).ToList();
                                    if (reverse.Count() > 0)
                                    {
                                        tickerActionString = reverse.FirstOrDefault().GetRecommendTickerAction();
                                    }
                                    foreach (var datum in reverse)
                                    {
                                        allIndicatorCrossed5 = datum.AllCrossesAbove5 || datum.AllCrossesBelow5;
                                        allIndicatorCrossed14 = datum.AllCrossesAbove14 || datum.AllCrossesBelow14;
                                        outputFile.WriteLine(datum.ToString());
                                    }
                                }
                            }
                            // this is because this list is for only one stock
                            if (allIndicatorCrossed5)
                            {
                                File.Move(fileName, Path.Combine(last5DaysCrossFolder, $"{tickerActionString}-{stock.Symbol}-{nowTime}.csv"));
                                continue;
                            }
                            if (allIndicatorCrossed14)
                            {
                                File.Move(fileName, Path.Combine(last14DaysCrossFolder, $"{tickerActionString}-{stock.Symbol}-{nowTime}.csv"));
                                continue;
                            }
                            File.Move(fileName, Path.Combine(scanFolderPath, $"{tickerActionString}-{stock.Symbol}-{nowTime}.csv"));
                        }
                        catch (Exception ex)
                        {
                            using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, $"error-{stock.Symbol}-{nowTime}.txt"), true))
                            {
                                outputFile.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                    Thread.Sleep(60000);
                }
            }
        }
         */
        #endregion

        private static void CreateScanDirectories(string folderPath, string scanFolderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (!Directory.Exists(scanFolderPath))
            {
                Directory.CreateDirectory(scanFolderPath);
            }
        }

        private static async Task<StockDataAggregator> RunScan(string ticker, string apiKey)
        {
            Console.WriteLine($"Getting data for {ticker}");
            string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?apikey={apiKey}";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                var tickerHistoricalPrices = JsonConvert.DeserializeObject<TickerHistoricalPrice>(content);

                if (tickerHistoricalPrices == null || tickerHistoricalPrices.Historical == null)
                {
                    return null;
                }

                Console.WriteLine($"Analyzing data for {ticker}");
                return new StockDataAggregator(tickerHistoricalPrices.Symbol, tickerHistoricalPrices.Historical, 5, 8, 21, 5, 14, 7 , 7);
            }
            return null;
        }

        public static async Task<IEnumerable<StockMeta>> GetStocksFromUSCANExchanges(long volumn, string apiKey)
        {
            List<string> exchanges = new List<string>() { "NYSE", "NasdaqNM", "AMEX", "TSX", "TSXV", "MX" };
            string baseUrl = "https://financialmodelingprep.com/api/v3";

            using (var httpClient = new HttpClient())
            {
                // Set the criteria for the search
                string url = $"{baseUrl}/stock-screener?volumeMoreThan={volumn}&apikey={apiKey}";

                // Send the request and get the response
                var response = await httpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var stocks = JArray.Parse(responseString).ToObject<List<StockMeta>>();

                // Filter the results by exchange short name
                var filteredStocks = stocks.Where(s => exchanges.Any(ex => ex.ToLower().Contains(s.ExchangeShortName.ToLower()))).ToList();

                return filteredStocks;
            }
        }
    }
}
