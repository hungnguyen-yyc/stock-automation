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
            var runStrategyStock = args.Any(a => a.ToLower().Contains("runstrategystock") || a.ToLower().Contains("run-strategy-stock"));
            var runScanStock = args.Any(a => a.ToLower().Contains("runscanstock") || a.ToLower().Contains("run-scan-stock"));
            var runScanStock15m = args.Any(a => a.ToLower().Contains("runscanstock15m") || a.ToLower().Contains("run-scan-stock-15m"));
            var runStrategyCrypto = args.Any(a => a.ToLower().Contains("runstrategycrypto") || a.ToLower().Contains("run-strategy-crypto"));
            var runScanCrypto = args.Any(a => a.ToLower().Contains("runscancrypto") || a.ToLower().Contains("run-scan-crypto"));
            // var failed = new List<string>() { "ATEST-A", "BTAL", "HIBS", "IIGD", "TOPS", "USFR", "WEBS" };
            var hot = new List<string>() { "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "GME", "AMC" };
            using (var httpClient = new HttpClient())
            {
                if (runScanStock15m)
                {
                    await StockScannerInterday.Run(hot.ToArray(), API_KEY);
                    return;
                }
                if (runStrategyStock || runScanStock)
                {
                    // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                    var northAmericaStocks = await StockScanner.GetStocksFromUSCANExchanges(999999999, 3000000, 1000, 10, API_KEY);

                    if (runStrategyStock)
                    {
                        await StockScanner.RunStrategyAnalysis(northAmericaStocks, API_KEY);
                    }
                    if (runScanStock)
                    {
                        await StockScanner.StartScan(northAmericaStocks.Where(s => hot.Contains(s.Symbol)), API_KEY);
                    }
                    return;
                }
                if (runStrategyCrypto || runScanCrypto)
                {
                    // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                    var availableCrytos = await GetAvailableCrytoCurrencies(API_KEY);

                    if (runStrategyCrypto)
                    {
                        //await RunStrategyAnalysis(availableCrytos, API_KEY);
                    }
                    if (runScanCrypto)
                    {
                        await StartScan(availableCrytos, API_KEY);
                    }
                    return;
                }
            }
        }

        public static async Task<IEnumerable<CryptoMeta>> GetAvailableCrytoCurrencies(string apiKey)
        {
            string baseUrl = $"https://financialmodelingprep.com/api/v3/symbol/available-cryptocurrencies?apikey={apiKey}";
            using (var httpClient = new HttpClient())
            {
                // Send the request and get the response
                var response = await httpClient.GetAsync(baseUrl);
                var responseString = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var cryptos = JArray.Parse(responseString).ToObject<List<CryptoMeta>>();

                return cryptos;
            }
        }

        private static async Task AnalyzeAndWriteCryptoData(CryptoMeta stock, string apiKey, string scanFolderPath, string nowTime)
        {
            try
            {
                Console.WriteLine($"Start processing for {stock.Name} - {stock.Symbol}");
                var data = await RunAnalysis(stock.Symbol, stock.ExchangeShortName, apiKey);
                if (data != null)
                {
                    var emaCrossBelow50200 = data.CheckEMACrossInLastNDays(7, 50, 200) == CrossDirection.CROSS_BELOW;
                    var emaCrossAbove50200 = data.CheckEMACrossInLastNDays(7, 50, 200) == CrossDirection.CROSS_ABOVE;

                    var emaCrossBelow1348 = data.CheckEMACrossInLastNDays(7, 13, 48) == CrossDirection.CROSS_BELOW;
                    var emaCrossAbove1348 = data.CheckEMACrossInLastNDays(7, 13, 48) == CrossDirection.CROSS_ABOVE;

                    var macdCrossAbove = data.GetMACDCrossDirectionInLastNDays(7) == CrossDirection.CROSS_ABOVE;
                    var macdCrossBelow = data.GetMACDCrossDirectionInLastNDays(7) == CrossDirection.CROSS_BELOW;
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

                    var trend = data.GetTrendInLastNDays(30);

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
                        if (trend == Trend.Uptrend)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-uptrend.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (trend == Trend.Downtrend)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-downtrend.txt");
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
                            if (adxInLast5Days)
                            {
                                var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-adx-25-rsi-bullish.txt");
                                WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                                if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                                {
                                    var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bullish.txt");
                                    WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                                }
                            }
                            if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                            {
                                var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-rsi-bullish.txt");
                                WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                                if (adxInLast5Days)
                                {
                                    var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bullish.txt");
                                    WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                                }
                            }
                        }
                        if (rsiBearish && emaCrossBelow1348)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-rsi-bearish.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                            if (adxInLast5Days)
                            {
                                var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-adx-25-rsi-bearish.txt");
                                WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                                if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                                {
                                    var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bearish.txt");
                                    WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                                }
                            }
                            if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                            {
                                var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-rsi-bearish.txt");
                                WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                                if (adxInLast5Days)
                                {
                                    var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bearish.txt");
                                    WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                                }
                            }
                        }
                        if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (trend == Trend.Uptrend)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-uptrend.txt");
                            WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                        }
                        if (trend == Trend.Downtrend)
                        {
                            var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-downtrend.txt");
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
            var folderPath = @"C:\Users\hnguyen\Documents\crypto-scan-logs";
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
                string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/4hour/{ticker}?apikey={apiKey}";

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
                    return new StockDataAggregator(tickerHistoricalPrices.Symbol, exchange, tickerHistoricalPrices.Historical, 14, 7, 12, 26, 9, 14, 7, 3, 14);
                }
            }
            catch (Exception ex)
            {
                var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
                var folderPath = @"C:\Users\hnguyen\Documents\crypto-scan-logs";
                var scanFolderPath = Path.Combine(folderPath, nowDate);
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, $"error-{ticker}.txt"), true))
                {
                    outputFile.WriteLine(ex.Message);
                    outputFile.WriteLine(ex.StackTrace);
                }
            }
            return null;
        }

        public static async Task StartScan(IEnumerable<CryptoMeta> allStocks, string apiKey)
        {
            string nowTime, scanFolderPath;
            CreateOutputFolder(out nowTime, out scanFolderPath);
            foreach (var stock in allStocks.OrderBy(a => a.Symbol))
            {
                await AnalyzeAndWriteCryptoData(stock, apiKey, scanFolderPath, nowTime);
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
    }
}
