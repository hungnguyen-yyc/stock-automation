using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using StockSignalScanner.Strategies;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace StockSignalScanner
{
    public partial class Program
    {
        static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";

        public static async Task Main(string[] args)
        {
            var runStrategy = args.Any(a => a.ToLower().Contains("runstrategy") || a.ToLower().Contains("run-strategy"));
            var runScan = args.Any(a => a.ToLower().Contains("runscan") || a.ToLower().Contains("run-scan"));
            using (var httpClient = new HttpClient())
            {
                // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                var northAmericaStocks = await GetStocksFromUSCANExchanges(999999999, 1000000, 1000, 0, API_KEY);

                if (runStrategy)
                {
                    await RunStrategyAnalysis(northAmericaStocks);
                }
                if (runScan)
                {
                    await StartScan(northAmericaStocks);
                }
            }
        }

        private static async Task RunStrategyAnalysis(IEnumerable<StockMeta> allStocks)
        {
            var batches = allStocks.OrderBy(s => s.Symbol).Chunk(250).ToArray();
            for (int si = 0; si < batches.Count(); si++)
            {
                var stocks = batches[si];
                foreach (var stock in stocks)
                {
                    var data = await RunAnalysis(stock.Symbol, stock.ExchangeShortName, API_KEY);
                    if (data != null)
                    {
                        EMACrossingStrategy.RunEMACross1334Strategy(data, 3, 7);
                        EMACrossingStrategy.RunEMACross1334WithAdxStrategy(data, 3, 7);
                        EMACrossingStrategy.RunEMACross21345589Strategy(data, 3, 7);
                        EMACrossingStrategy.RunEMACross21345589WithAdxStrategy(data, 3, 7);
                        EMACrossingStrategy.RunEMACross50200Strategy(data, 3, 7);
                        EMACrossingStrategy.RunEMACross50200WithAdxStrategy(data, 3, 7);
                    }
                }
                // Thread.Sleep(60000);
            }
        }

        private static async Task StartScan(IEnumerable<StockMeta> allStocks)
        {
            string nowTime, scanFolderPath;
            CreateOutputFolder(out nowTime, out scanFolderPath);
            foreach (var stock in allStocks.OrderBy(a => a.Symbol))
            {
                await AnalyzeAndWriteStockData(stock, scanFolderPath, nowTime);
            }
        }

        private static async Task AnalyzeAndWriteStockData(StockMeta stock, string scanFolderPath, string nowTime)
        {
            try
            {
                Console.WriteLine($"Start processing for {stock.Name} - {stock.Symbol}");
                var data = await RunAnalysis(stock.Symbol, stock.ExchangeShortName, API_KEY);
                if (data != null)
                {
                    var emaCrossBelow50200 = data.CheckEMACrossInLastNDays(5, 50, 200) == CrossDirection.CROSS_BELOW
                        && data.CheckEMACrossInLastNDays(5, 20, 200) == CrossDirection.CROSS_BELOW;
                    var emaCrossAbove50200 = data.CheckEMACrossInLastNDays(3, 50, 200) == CrossDirection.CROSS_ABOVE
                        && data.CheckEMACrossInLastNDays(5, 50, 200) == CrossDirection.CROSS_ABOVE;

                    var emaCrossBelow1348 = data.CheckEMACrossInLastNDays(5, 13, 48) == CrossDirection.CROSS_BELOW;
                    var emaCrossAbove1348 = data.CheckEMACrossInLastNDays(5, 13, 48) == CrossDirection.CROSS_ABOVE;

                    var macdCrossAbove = data.GetMACDCrossDirectionInLastNDays(5) == CrossDirection.CROSS_ABOVE;
                    var macdCrossBelow = data.GetMACDCrossDirectionInLastNDays(5) == CrossDirection.CROSS_BELOW;
                    var notOverbought = !data.IsOverboughtByStochasticInLastNDays(5)
                        && !data.IsOverboughtByRSIInLastNDays(10);
                    var notOversold = !data.IsOversoldByStochasticInLastNDays(5)
                        && !data.IsOversoldByRSIInLastNDays(10);

                    var rsiBullish = data.IsBullishByRSIInLastNDays(5);
                    var rsiBearish = data.IsBullishByRSIInLastNDays(5);

                    var last3DayHistogramIncrease = true;
                    var histogramInLast3Days = data.GetMACDHistogramInLastNDays(3).ToList();
                    for (int i = 1; i < histogramInLast3Days.Count(); i++)
                    {
                        var prev = histogramInLast3Days[i - 1];
                        if (histogramInLast3Days[i] < prev)
                        {
                            last3DayHistogramIncrease = false;
                            break;
                        }
                    }

                    var adxInLast5Days = data.GetADXInLastNDays(3).All(x => x > 25);
                    if (emaCrossAbove50200 || emaCrossBelow50200)
                    {
                        var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-50-200.txt");
                        WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5)); 
                        if (adxInLast5Days)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-adx-25.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (rsiBullish && emaCrossAbove50200)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-rsi-bullish.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (rsiBearish && emaCrossBelow50200)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-rsi-bearish.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-touch-20.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                    }
                    if (emaCrossAbove1348 || emaCrossBelow1348)
                    {
                        var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-13-48.txt");
                        WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
                        if (adxInLast5Days)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-adx-25.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (rsiBullish && emaCrossAbove1348)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-rsi-bullish.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (rsiBearish && emaCrossBelow1348)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-rsi-bearish.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                    }
                    if (emaCrossAbove1348 && notOverbought)
                    {
                        var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-above-13-48-notoverbought.txt");
                        WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(10));

                        if (adxInLast5Days && macdCrossAbove)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-notoverbought-macd-cross-above-adx-25.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                    }
                    if (emaCrossBelow1348 && notOversold)
                    {
                        var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-below-13-48-notoversold.txt");
                        WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(10));

                        if (adxInLast5Days && macdCrossAbove)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-notoversold-macd-cross-below-adx-25.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
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

        private static void CreateOutputFolder(out string nowTime, out string scanFolderPath)
        {
            nowTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
            var folderPath = @"C:\Users\hnguyen\Documents\stock-scan-logs";
            scanFolderPath = Path.Combine(folderPath, nowDate);
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
        }

        private static void WriteToFile(string filePath, string content, string header = "Ticker,Exchange,MACD,RSI,STOCHASTICS,PATTERNS,PRICES(Last 5 days),Fibonacci(30)")
        {
            if (!File.Exists(filePath))
            {
                File.AppendAllLines(filePath, new[] { header });
            }
            File.AppendAllLines(filePath, new[] { content });
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
            try
            {
                Console.WriteLine($"Getting data for {ticker}");
                var ago7years = DateTime.Now.AddYears(-10).ToString("yyyy-MM-dd");
                string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?from={ago7years}&apikey={apiKey}";

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
                    return new StockDataAggregator(tickerHistoricalPrices.Symbol, exchange, tickerHistoricalPrices.Historical, 14, 7, 12, 26, 9, 14, 7, 3);
                }
            }
            catch (Exception ex)
            {
                var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
                var folderPath = @"C:\Users\hnguyen\Documents\stock-scan-logs";
                var scanFolderPath = Path.Combine(folderPath, nowDate);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, $"error-{ticker}.txt"), true))
                {
                    outputFile.WriteLine(ex.Message);
                    outputFile.WriteLine(ex.StackTrace);
                }
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
                string url = $"{baseUrl}/stock-screener?volumeMoreThan={volumeMin}&volumeLowerThan={volumeMax}&priceLowerThan={priceMax}&priceMoreThan={priceMin}&isActivelyTrading=true&limit={int.MaxValue}&apikey={apiKey}";

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
