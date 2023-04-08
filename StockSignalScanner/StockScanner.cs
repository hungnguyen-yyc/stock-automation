using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockSignalScanner.Models;
using StockSignalScanner.Strategies;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace StockSignalScanner
{
    internal static class StockScanner
    {

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

        private static async Task AnalyzeAndWriteStockData(StockMeta stock, string apiKey, string scanFolderPath, string nowTime)
        {
            try
            {
                Console.WriteLine($"Start processing for {stock.Name} - {stock.Symbol}");
                var data = await StockScanner.RunAnalysis(stock.Symbol, stock.ExchangeShortName, apiKey);
                if (data != null)
                {
                    RunAroonLeadStrategy(data, scanFolderPath);
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

        private static void RunAroonLeadStrategy(StockDataAggregator data, string scanFolderPath)
        {
            var prices = data.PriceOrderByDateAsc;
            var strategy = new AroonLeadStrategy(prices, new IndicatorParameterPackage
            {
                Rsi = 14,
                Mfi = 14,
                StcCycleLength = 10,
                StcFactor = 0.5,
                StcLong = 50,
                StcShort = 23,
                MovingAverage = 14,
                AroonOscillator = 14,
            }, 5);
            var rating = strategy.GetRating(); 
            var details = strategy.GetDetails();
            var description = $"- {data.Symbol}: {rating}";
            description += "\n";
            description += details;
            description += "\n";
            description += "---------------------------------------------------------";
            description += "\n";

            if (rating == 100)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ratings100.txt");
                WriteToFile(pathToWrite, description);

                return;
            }

            if (rating >= 90)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ratings90.txt");
                WriteToFile(pathToWrite, description);

                return;
            }

            if (rating >= 80)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ratings80.txt");
                WriteToFile(pathToWrite, description);

                return;
            }
        }

        private static void RunStcAnalysis(StockDataAggregator data, string scanFolderPath)
        {
            var strongADX = data.GetADXInLastNDays(1).All(x => x > 25);
            var stcCross25 = data.CheckSTCCrossInLastNDays(25, 3, 23, 50, 10, 0.5m);
            var stcCross75 = data.CheckSTCCrossInLastNDays(75, 3, 23, 50, 10, 0.5m);
            var emaCross58 = data.CheckEMACrossInLastNDays(5, 5, 8, 2);
            var emaCross813 = data.CheckEMACrossInLastNDays(5, 8, 13, 2);
            var mfi = data.GetMFICrossLevelInLastNDays(14, 50, 5);
            var aroon = data.GetAroonOscillatorCrossLevelInLastNDays(14, 50, 5);
            var smi = data.GeStochasticMomentumIndexCrossLevelInLastNDays(14, 50, 5);
            var vwma = data.GetVolumeWeightedMovingAverageInLastNDays(20);

            if(emaCross58 == CrossDirection.CROSS_ABOVE && emaCross813 == CrossDirection.CROSS_ABOVE)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ema-cross-above-5-8-13.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(3));
            }
            if (emaCross58 == CrossDirection.CROSS_BELOW && emaCross813 == CrossDirection.CROSS_BELOW)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ema-cross-below-5-8-13.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(3));
            }

            if (stcCross25 == CrossDirection.CROSS_ABOVE)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "buy-stc-last-5.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(3));
                if (strongADX)
                {
                    pathToWrite = Path.Combine(scanFolderPath, "buy-stc-adx-last-5.txt");
                    StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(3));
                }
            }

            if (stcCross75 == CrossDirection.CROSS_BELOW && strongADX)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "sell-stc-last-5.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(3));
                if (strongADX)
                {
                    pathToWrite = Path.Combine(scanFolderPath, "sell-stc-adx-last-5.txt");
                    StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(3));
                }
            }
        }

        private static void RunMacdAnalysis(StockDataAggregator data, string scanFolderPath)
        {
            var macd = data.GetMACDHistogramInLastNDays(7);
            var macdCrossZeroDirection = data.GetMACDCrossZeroDirectionInLastNDays(7);

            var macdCrossAboveLast5 = data.GetMACDCrossDirectionInLastNDays(5) == CrossDirection.CROSS_ABOVE;
            var macdCrossBelowLast5 = data.GetMACDCrossDirectionInLastNDays(5) == CrossDirection.CROSS_BELOW;
            var macdCrossAboveLast15 = data.GetMACDCrossDirectionInLastNDays(15) == CrossDirection.CROSS_ABOVE;
            var macdCrossBelowLast15 = data.GetMACDCrossDirectionInLastNDays(15) == CrossDirection.CROSS_BELOW;
            var notOverbought = !data.IsOverboughtByStochasticInLastNDays(5)
                && !data.IsOverboughtByRSIInLastNDays(5);
            var notOversold = !data.IsOversoldByStochasticInLastNDays(5)
                && !data.IsOversoldByRSIInLastNDays(5);

            var rsiBullish = data.IsBullishByRSIInLastNDays(5);
            var rsiBearish = data.IsBullishByRSIInLastNDays(5);

            var positiveHistogram = data.GetMACDHistogramInLastNDays(5).ToList().All(i => i > 0);
            var negativeHistogram = data.GetMACDHistogramInLastNDays(5).ToList().All(i => i < 0);

            var strongADX = data.GetADXInLastNDays(3).All(x => x > 20);
            var stcCross25 = data.CheckSTCCrossInLastNDays(25, 3, 55, 144, 10, 0.5m);
            var stcCross75 = data.CheckSTCCrossInLastNDays(75, 3, 55, 144, 10, 0.5m);
            var hasDIPlusAboveDmiMinusLast3Days = data.Dmi.HasDIPlusAboveMinusInLastNDays(1);
            var hasDIPlusBelowDmiMinusLast3Days = data.Dmi.HasDIPlusBelowMinusInLastNDays(1);

            var stocCross = data.GetStochasticCrossDirectionInLastNDays(7);

            if (data.HasOverboughtOrOversoldFollowedByMACDCrossLastNDays(7, 5))
            {
                var pathToWrite = Path.Combine(scanFolderPath, "macd-stochatic-last-5.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
                if (data.IsOversoldByRSIInLastNDays(10) || data.IsOverboughtByRSIInLastNDays(10))
                {
                    pathToWrite = Path.Combine(scanFolderPath, "macd-stochatic-rsi-last-5.txt");
                    StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
                }
            }

            if (positiveHistogram
                && strongADX
                && rsiBullish
                && macdCrossAboveLast5
                && macdCrossZeroDirection == CrossDirection.CROSS_ABOVE
                && stocCross == CrossDirection.CROSS_ABOVE)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "all-indicators-bull.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
            }

            if (negativeHistogram
                && strongADX
                && rsiBearish
                && macdCrossBelowLast5
                && macdCrossZeroDirection == CrossDirection.CROSS_BELOW
                && stocCross == CrossDirection.CROSS_BELOW)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "all-indicators-bear.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
            }
        }

        private static void RunEMACrossAnalysis(
            StockDataAggregator data, 
            string scanFolderPath, 
            bool strongADX, 
            bool macdCrossAboveLast15, 
            bool rsiBullish, 
            bool rsiBearish, 
            bool macdCrossBelowLast15,
            bool notOverbought,
            bool notOversold,
            bool macdCrossAboveLast5,
            bool macdCrossBelowLast5
            )
        {
            var emaCrossBelow50200 = data.CheckEMACrossInLastNDays(7, 50, 200, 2) == CrossDirection.CROSS_BELOW;
            var emaCrossAbove50200 = data.CheckEMACrossInLastNDays(7, 50, 200, 2) == CrossDirection.CROSS_ABOVE;

            var emaCrossBelow1348 = data.CheckEMACrossInLastNDays(5, 13, 48, 2) == CrossDirection.CROSS_BELOW;
            var emaCrossAbove1348 = data.CheckEMACrossInLastNDays(5, 13, 48, 2) == CrossDirection.CROSS_ABOVE;

            if (emaCrossAbove50200 || emaCrossBelow50200)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-50-200.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
                if (strongADX)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-adx-25.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                if (rsiBullish && emaCrossAbove50200)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-rsi-bullish.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                if (rsiBearish && emaCrossBelow50200)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-rsi-bearish.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                if (macdCrossBelowLast15 && emaCrossBelow50200)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-macd-bearish.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                if (macdCrossAboveLast15 && emaCrossAbove50200)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-macd-bullish.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-touch-20.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                //if (trend == Trend.Uptrend)
                //{
                //    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-uptrend.txt");
                //    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                //}
                //if (trend == Trend.Downtrend)
                //{
                //    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-50-200-downtrend.txt");
                //    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                //}
            }
            if (emaCrossAbove1348 || emaCrossBelow1348)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-13-48.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(5));
                if (strongADX)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-adx-25.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
                if (rsiBullish && emaCrossAbove1348)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-rsi-bullish.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                    if (strongADX)
                    {
                        var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-adx-25-rsi-bullish.txt");
                        StockScanner.WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                        if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                        {
                            var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bullish.txt");
                            StockScanner.WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                        }
                    }
                    if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                    {
                        var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-rsi-bullish.txt");
                        StockScanner.WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                        if (strongADX)
                        {
                            var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bullish.txt");
                            StockScanner.WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                        }
                    }
                }
                if (rsiBearish && emaCrossBelow1348)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-rsi-bearish.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                    if (strongADX)
                    {
                        var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-adx-25-rsi-bearish.txt");
                        StockScanner.WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                        if (data.CheckPriceTouchEMAInLastNDays(5, 20))
                        {
                            var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bearish.txt");
                            StockScanner.WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                        }
                    }
                    if (data.CheckPriceTouchEMAInLastNDays(5, 20))
                    {
                        var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-rsi-bearish.txt");
                        StockScanner.WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                        if (strongADX)
                        {
                            var emacross3 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20-adx-25-rsi-bearish.txt");
                            StockScanner.WriteToFile(emacross3, data.GetTickerStatusLastNDays(5));
                        }
                    }
                }
                if (data.CheckPriceTouchEMAInLastNDays(3, 20))
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-touch-20.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                }
            }
            if (emaCrossAbove1348 && notOverbought)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-above-13-48-notoverbought.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(10));

                if (strongADX)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-notoverbought-adx-25.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                    if (macdCrossAboveLast5)
                    {
                        var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-notoverbought-macd-cross-above-adx-25.txt");
                        StockScanner.WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                    }
                }
            }
            if (emaCrossBelow1348 && notOversold)
            {
                var pathToWrite = Path.Combine(scanFolderPath, "ema-crosses-below-13-48-notoversold.txt");
                StockScanner.WriteToFile(pathToWrite, data.GetTickerStatusLastNDays(10));

                if (strongADX)
                {
                    var emacross1 = Path.Combine(scanFolderPath, "ema-crosses-13-48-notoversold-adx-25.txt");
                    StockScanner.WriteToFile(emacross1, data.GetTickerStatusLastNDays(5));
                    if (macdCrossBelowLast5)
                    {
                        var emacross2 = Path.Combine(scanFolderPath, "ema-crosses-13-48-notoversold-macd-cross-below-adx-25.txt");
                        StockScanner.WriteToFile(emacross2, data.GetTickerStatusLastNDays(5));
                    }
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
                var ago7years = DateTime.Now.AddYears(-5).ToString("yyyy-MM-dd");
                string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?from={ago7years}&apikey={apiKey}";

                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var tickerHistoricalPrices = JsonConvert.DeserializeObject<TickerHistoricalPrice>(content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (tickerHistoricalPrices == null || tickerHistoricalPrices.Historical == null || tickerHistoricalPrices.Historical.Count < 750)
                    {
                        return null;
                    }

                    Console.WriteLine($"Analyzing data for {ticker}");
                    return new StockDataAggregator(tickerHistoricalPrices.Symbol, exchange, tickerHistoricalPrices.Historical, 14, 7, 12, 26, 9, 14, 7, 3, 21);
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

        public static async Task StartScan(IEnumerable<StockMeta> allStocks, string apiKey)
        {
            string nowTime, scanFolderPath;
            CreateOutputFolder(out nowTime, out scanFolderPath);
            foreach (var stock in allStocks.OrderBy(a => a.Symbol))
            {
                await AnalyzeAndWriteStockData(stock, apiKey, scanFolderPath, nowTime);
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