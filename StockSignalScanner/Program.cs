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
                var northAmericaStocks = await GetStocksFromUSCANExchanges(100000000, 1000000, 20, 0, API_KEY);

                var random = new Random();
                var nowTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
                var folderPath = @"C:\Users\hnguyen\Documents\stock-scan-logs";
                var scanFolderPath = Path.Combine(folderPath, nowDate);
                var inSupportZone90 = new List<string>();
                var inSupportZone180 = new List<string>();
                var inSupportZone360 = new List<string>();
                var inSupportZone540 = new List<string>();
                var inResistanceZone90 = new List<string>();
                var inResistanceZone180 = new List<string>();
                var inResistanceZone360 = new List<string>();
                var inResistanceZone540 = new List<string>();
                var trendlineHighs180 = new List<string>();
                var fibonacciLast180s = new List<string>();
                var fibonacciLast90s = new List<string>();
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
                            var data = await RunAnalysis(stock.Symbol, stock.ExchangeShortName, API_KEY);
                            if (data != null)
                            {
                                var supportZoneState90 = data.CheckInSupportZoneLastNDays(90);
                                var supportZoneState180 = data.CheckInSupportZoneLastNDays(180);
                                var supportZoneState360 = data.CheckInSupportZoneLastNDays(360);
                                var supportZoneState540 = data.CheckInSupportZoneLastNDays(540);
                                var resistanceZoneState90 = data.CheckInResistanceZoneLastNDays(90);
                                var resistanceZoneState180 = data.CheckInResistanceZoneLastNDays(180);
                                var resistanceZoneState360 = data.CheckInResistanceZoneLastNDays(360);
                                var resistanceZoneState540 = data.CheckInResistanceZoneLastNDays(540);
                                var trendlineHigh180 = data.TrendlineHigh(180);
                                var fibonacciLast180 = data.GetCurrentFibonacciRetracementLevelLastNDays(180);
                                var fibonacciLast90 = data.GetCurrentFibonacciRetracementLevelLastNDays(90);

                                if (fibonacciLast180 != null)
                                {
                                    fibonacciLast180s.Add($"{data.Symbol}-{fibonacciLast180.ToString()}");
                                }
                                if (fibonacciLast90 != null)
                                {
                                    fibonacciLast90s.Add($"{data.Symbol}-{fibonacciLast90.ToString()}");
                                }

                                if (trendlineHigh180.Length > 0)
                                {
                                    trendlineHighs180.Add($"{data.Symbol}-{trendlineHigh180}");
                                }
                                if (supportZoneState90.IsInZone || supportZoneState90.IsAboutEnterTheZone || supportZoneState90.IsAboutOutOfTheZone)
                                {
                                    inSupportZone90.Add($"{data.Symbol}-{supportZoneState90.IsInZone}-{supportZoneState90.IsAboutOutOfTheZone}-{supportZoneState90.IsAboutEnterTheZone}-{supportZoneState90.ZoneHigh}-{supportZoneState90.ZoneLow}");
                                }
                                if (supportZoneState180.IsInZone || supportZoneState180.IsAboutEnterTheZone || supportZoneState180.IsAboutOutOfTheZone)
                                {
                                    inSupportZone180.Add($"{data.Symbol}-{supportZoneState180.IsInZone}-{supportZoneState180.IsAboutOutOfTheZone}-{supportZoneState180.IsAboutEnterTheZone}-{supportZoneState180.ZoneHigh}-{supportZoneState180.ZoneLow}");
                                }
                                if (supportZoneState360.IsInZone || supportZoneState360.IsAboutEnterTheZone || supportZoneState360.IsAboutOutOfTheZone)
                                {
                                    inSupportZone360.Add($"{data.Symbol}-{supportZoneState360.IsInZone}-{supportZoneState360.IsAboutOutOfTheZone}-{supportZoneState360.IsAboutEnterTheZone}-{supportZoneState360.ZoneHigh}-{supportZoneState360.ZoneLow}");
                                }
                                if (supportZoneState540.IsInZone || supportZoneState540.IsAboutEnterTheZone || supportZoneState540.IsAboutOutOfTheZone)
                                {
                                    inSupportZone540.Add($"{data.Symbol}-{supportZoneState540.IsInZone}-{supportZoneState540.IsAboutOutOfTheZone}-{supportZoneState540.IsAboutEnterTheZone}-{supportZoneState540.ZoneHigh}-{supportZoneState540.ZoneLow}");
                                }

                                if (resistanceZoneState90.IsInZone || resistanceZoneState90.IsAboutEnterTheZone || resistanceZoneState90.IsAboutOutOfTheZone)
                                {
                                    inResistanceZone90.Add($"{data.Symbol}-{resistanceZoneState90.IsInZone}-{resistanceZoneState90.IsAboutOutOfTheZone}-{resistanceZoneState90.IsAboutEnterTheZone}-{resistanceZoneState90.ZoneHigh}-{resistanceZoneState90.ZoneLow}");
                                }
                                if (resistanceZoneState180.IsInZone || resistanceZoneState180.IsAboutEnterTheZone || resistanceZoneState180.IsAboutOutOfTheZone)
                                {
                                    inResistanceZone180.Add($"{data.Symbol}-{resistanceZoneState180.IsInZone}-{resistanceZoneState180.IsAboutOutOfTheZone}-{resistanceZoneState180.IsAboutEnterTheZone}-{resistanceZoneState180.ZoneHigh}-{resistanceZoneState180.ZoneLow}");
                                }
                                if (resistanceZoneState360.IsInZone || resistanceZoneState360.IsAboutEnterTheZone || resistanceZoneState360.IsAboutOutOfTheZone)
                                {
                                    inResistanceZone360.Add($"{data.Symbol}-{resistanceZoneState360.IsInZone}-{resistanceZoneState360.IsAboutOutOfTheZone}-{resistanceZoneState360.IsAboutEnterTheZone}-{resistanceZoneState360.ZoneHigh}-{resistanceZoneState360.ZoneLow}");
                                }
                                if (resistanceZoneState540.IsInZone || resistanceZoneState540.IsAboutEnterTheZone || resistanceZoneState540.IsAboutOutOfTheZone)
                                {
                                    inResistanceZone540.Add($"{data.Symbol}-{resistanceZoneState540.IsInZone}-{resistanceZoneState540.IsAboutOutOfTheZone}-{resistanceZoneState540.IsAboutEnterTheZone}-{resistanceZoneState540.ZoneHigh}-{resistanceZoneState540.ZoneLow}");
                                }

                                if (data.HasOverboughtOrOversoldFollowedByMACDCrossLastNDays())
                                {
                                    var overboughtOrOversoldFollowedByMACDCrossLast5DaysFile = Path.Combine(scanFolderPath, "overbought-or-oversold-with-macd-cross.txt");
                                    WriteToFile(overboughtOrOversoldFollowedByMACDCrossLast5DaysFile, data.GetTickerStatusLastNDays(5));
                                }
                                if ((data.CheckAllCrossesWithDirectionInLastNDays(5, CrossDirection.CROSS_ABOVE) && data.IsOversoldByStochasticInLastNDays(5))
                                    || data.CheckAllCrossesWithDirectionInLastNDays(5, CrossDirection.CROSS_BELOW) && data.IsOverboughtByStochasticInLastNDays(5))
                                {
                                    if (data.CheckInSupportZoneLastNDays(30).IsInZone 
                                        || data.CheckInSupportZoneLastNDays(90).IsInZone 
                                        || data.CheckInSupportZoneLastNDays(180).IsInZone)
                                    {
                                        var allCrossesInZoneFile = Path.Combine(scanFolderPath, "in-zone-all-crosses-last-5-days.txt");
                                        WriteToFile(allCrossesInZoneFile, data.GetTickerStatusLastNDays(5));
                                    }
                                    var allCrossesFile = Path.Combine(scanFolderPath, "all-crosses-last-5-days.txt");
                                    WriteToFile(allCrossesFile, data.GetTickerStatusLastNDays(5));
                                    if (data.IsOversoldByRSIInLastNDays(5) || data.IsOverboughtByRSIInLastNDays(5))
                                    {
                                        var allCrossesInZoneFile = Path.Combine(scanFolderPath, "strict-all-crosses-last-5-days.txt");
                                        WriteToFile(allCrossesInZoneFile, data.GetTickerStatusLastNDays(5));
                                    }
                                }
                                else
                                {
                                    mix.Add(data.GetTickerStatusLastNDays(5));
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
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "support-zone-180.txt"), true))
                {
                    foreach (var item in inSupportZone180)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "support-zone-90.txt"), true))
                {
                    foreach (var item in inSupportZone90)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "support-zone-360.txt"), true))
                {
                    foreach (var item in inSupportZone360)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "support-zone-540.txt"), true))
                {
                    foreach (var item in inSupportZone540)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "resistance-zone-180.txt"), true))
                {
                    foreach (var item in inResistanceZone180)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "resistance-zone-90.txt"), true))
                {
                    foreach (var item in inResistanceZone90)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "resistance-zone-360.txt"), true))
                {
                    foreach (var item in inResistanceZone360)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "resistance-zone-540.txt"), true))
                {
                    foreach (var item in inResistanceZone540)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "fibonacci-180.txt"), true))
                {
                    foreach (var item in fibonacciLast180s)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "fibonacci-90.txt"), true))
                {
                    foreach (var item in fibonacciLast90s)
                    {
                        outputFile.WriteLine(item);
                    }
                }
            }
        }

        private static void WriteToFile(string filePath, string content)
        {
            if (!File.Exists(filePath))
            {
                File.AppendAllLines(filePath, new[] { "Ticker,Exchange,MACD,RSI,STOCHASTICS,PATTERNS,PRICES(Last 5 days),Fibonacci(90)" });
            }
            File.AppendAllLines(filePath, new[] { content });
            var dir = Path.GetDirectoryName(filePath);
            var questrade = Path.Combine(dir, "questrade-watchlist-combined.csv");
            if (!File.Exists(questrade))
            {
                File.AppendAllLines(questrade, new[] { "\"Symbol\"" });
            }
            File.AppendAllLines(questrade, new[] { content.Split(",")[0] });
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

        private static async Task<StockDataAggregator> RunAnalysis(string ticker, string exchange, string apiKey)
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
                return new StockDataAggregator(tickerHistoricalPrices.Symbol, exchange, tickerHistoricalPrices.Historical, 8, 5, 8, 21, 5, 8, 5, 3);
            }
            return null;
        }

        public static async Task<IEnumerable<StockMeta>> GetStocksFromUSCANExchanges(long volumeMax, long volumeMin, long priceMax, long priceMin, string apiKey)
        {
            List<string> exchanges = new List<string>() { "NYSE", "NasdaqNM", "AMEX", "TSX", "TSXV", "MX" };
            string baseUrl = "https://financialmodelingprep.com/api/v3";

            using (var httpClient = new HttpClient())
            {
                // Set the criteria for the search
                string url = $"{baseUrl}/stock-screener?volumeMoreThan={volumeMin}&volumeLowerThan={volumeMax}&priceLowerThan={priceMax}&priceMoreThan={priceMin}&isActivelyTrading=true&apikey={apiKey}";

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
