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
        private static readonly int[] CROSSES_IN_LAST_DAYS = new int[] { 14, 5 }; // TODO: update StockData to handle crosses value better

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
                var rsiMacdCrosses5 = new List<string>();
                var allCrosses5 = new List<string>();
                var allCrosses5WithStochastic = new List<string>();
                var rsiMacdCrosses14 = new List<string>();
                var allCrosses14 = new List<string>();
                var allCrosses14WithStochastic = new List<string>();
                var squeezeMomentum = new List<string>();
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
                            var data = await RunScan(stock.Symbol, stock.ExchangeShortName, API_KEY);
                            if (data != null)
                            {
                                var reverse = data.OrderByDescending(d => d.Date).ToList();
                                if (reverse.Count() > 0)
                                {
                                    var datum = reverse.FirstOrDefault();
                                    if (datum != null)
                                    {

                                        if (datum.MACDRSICrossesAbove5WithStochastic || datum.MACDRSICrossesBelow5WithStochastic)
                                        {
                                            rsiMacdCrosses5.Add(datum.GetRecommendTickerAction());
                                        }
                                        if (datum.MACDRSICrossesAbove14WithStochastic || datum.MACDRSICrossesBelow14WithStochastic)
                                        {
                                            rsiMacdCrosses14.Add(datum.GetRecommendTickerAction());
                                        }
                                        if (datum.AllCrossesAbove5 || datum.AllCrossesBelow5)
                                        {
                                            allCrosses5.Add(datum.GetRecommendTickerAction());
                                            if (datum.AllCrossesAbove5WithStochastic || datum.AllCrossesBelow5WithStochastic)
                                            {
                                                allCrosses5WithStochastic.Add(datum.GetRecommendTickerAction());
                                            }
                                        }
                                        else if (datum.AllCrossesAbove14 || datum.AllCrossesBelow14)
                                        {
                                            allCrosses14.Add(datum.GetRecommendTickerAction());
                                            if (datum.AllCrossesAbove14WithStochastic || datum.AllCrossesBelow14WithStochastic)
                                            {
                                                allCrosses14WithStochastic.Add(datum.GetRecommendTickerAction());
                                            }
                                        }
                                        else
                                        {
                                            mix.Add(datum.GetRecommendTickerAction());
                                        }
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
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "all-crosses-last-5-days-with-stochastic.txt"), true))
                {
                    foreach (var item in allCrosses5WithStochastic)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "all-crosses-last-14-days-with-stochastic.txt"), true))
                {
                    foreach (var item in allCrosses14WithStochastic)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "rsi-macd-crosses-last-5-days-with-stochastic.txt"), true))
                {
                    foreach (var item in rsiMacdCrosses5)
                    {
                        outputFile.WriteLine(item);
                    }
                }
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, "rsi-macd-crosses-last-14-days-with-stochastic.txt"), true))
                {
                    foreach (var item in rsiMacdCrosses14)
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
                Process.Start("explorer.exe", scanFolderPath);
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

        private static async Task<List<StockData>> RunHourScan(string ticker, string exchange, string apiKey)
        {
            string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/1hour/{ticker}?apikey={apiKey}";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                var prices = JArray.Parse(content).ToObject<List<IPrice>>();

                if (prices == null)
                {
                    return null;
                }

                var reverse = prices.OrderBy(p => p.Date).ToList();
                return GetIndicators(ticker, exchange, reverse, 5, 8, 21, 5, 5);
            }
            return null;
        }

        private static async Task<List<StockData>> RunScan(string ticker, string exchange, string apiKey)
        {
            Console.WriteLine($"Getting data for {ticker}");
            string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?apikey={apiKey}";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                var prices = JsonConvert.DeserializeObject<TickerHistoricalPrice>(content);

                if (prices == null || prices.Historical == null)
                {
                    return null;
                }

                var reverse = prices.Historical
                    .OrderBy(p => p.Date)
                    .Select(p => (IPrice)new HistoricalPrice
                    {
                        Date = p.Date,
                        AdjClose = p.AdjClose,
                        Close = p.Close,
                        Change = p.Change,
                        ChangeOverTime = p.ChangeOverTime,
                        ChangePercent = p.ChangePercent,
                        High = p.High,
                        Label = p.Label,
                        Low = p.Low,
                        Open = p.Open,
                        UnadjustedVolume = p.UnadjustedVolume,
                        Volume = p.Volume,
                        Vwap = p.Vwap,
                    })
                    .ToList();
                Console.WriteLine($"Analyzing data for {ticker}");
                return GetIndicators(ticker, exchange, reverse, 14, 12, 26, 9, 14);
            }
            return null;
        }

        public static List<StockData> GetIndicators(string ticker, string exchange, List<IPrice> prices, int rsiPeriod, int macdShortPeriod, int macdLongPeriod, int macdSignalPeriod, int stochasticPeriod)
        {
            var result = new List<StockData>();

            // Get the RSI, MACD, and stochastic values and times
            (List<decimal> rsiValues, List<DateTime> rsiTimes) = RSIIndicator.GetRSI(prices, rsiPeriod);
            (List<decimal> macdValues, List<decimal> macdSignalValues, List<DateTime> macdTimes) = MACDIndicator.GetMACD(prices, macdShortPeriod, macdLongPeriod, macdSignalPeriod);
            (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) = StochasticIndicator.GetStochastic(prices, stochasticPeriod);
            //var squeezeMomentum = null;// SqueezeMomentumIndicator.Calculate(prices, 14, 2, 14, 14);
            var squeezeMomentumV3 = SqueezeMomentumIndicator.CalculateV3(prices, 20, 2, 20, 1.5, true);

            CrossDirection macdCrossCheck14 = CrossDirection.NO_CROSS;
            CrossDirection stochCrossCheck14 = CrossDirection.NO_CROSS;
            CrossDirection rsiCrossCheck14 = CrossDirection.NO_CROSS;
            CrossDirection macdCrossCheck5 = CrossDirection.NO_CROSS;
            CrossDirection stochCrossCheck5 = CrossDirection.NO_CROSS;
            CrossDirection rsiCrossCheck5 = CrossDirection.NO_CROSS;
            var stochasticInOverboughtLast5Days = false;
            var stochasticInOversoldLast5Days = false;
            var stochasticInOverboughtLast14Days = false;
            var stochasticInOversoldLast14Days = false;

            foreach (var days in CROSSES_IN_LAST_DAYS)
            {
                List<(DateTime, decimal)> macdLine = macdTimes.Zip(macdValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> signalLine = macdTimes.Zip(macdSignalValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> kLine = stochasticTimes.Zip(kValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> dLine = stochasticTimes.Zip(dValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> rsiLine = rsiTimes.Zip(rsiValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> rsi50Line = rsiTimes.Select(r => (r, 50m)).Take(days).ToList();

                // base on this https://www.youtube.com/watch?v=R1cKTKV6-gc

                if (days == 14)
                {
                    macdCrossCheck14 = CrossDirectionDetector.GetCrossDirection(macdLine, signalLine);
                    stochCrossCheck14 = CrossDirectionDetector.GetCrossDirection(kLine, dLine);
                    rsiCrossCheck14 = CrossDirectionDetector.GetCrossDirection(rsiLine, rsi50Line);
                    stochasticInOverboughtLast14Days = kLine.Select(k => k.Item2).Any(k => k >= 80) && dLine.Select(k => k.Item2).Any(k => k >= 80);
                    stochasticInOversoldLast14Days = kLine.Select(k => k.Item2).Any(k => k <= 20) && dLine.Select(k => k.Item2).Any(k => k <= 20);
                }
                if (days == 5)
                {
                    macdCrossCheck5 = CrossDirectionDetector.GetCrossDirection(macdLine, signalLine);
                    stochCrossCheck5 = CrossDirectionDetector.GetCrossDirection(kLine, dLine);
                    rsiCrossCheck5 = CrossDirectionDetector.GetCrossDirection(rsiLine, rsi50Line);
                    stochasticInOverboughtLast5Days = kLine.Select(k => k.Item2).Any(k => k >= 80) && dLine.Select(k => k.Item2).Any(k => k >= 80);
                    stochasticInOverboughtLast5Days = kLine.Select(k => k.Item2).Any(k => k <= 20) && dLine.Select(k => k.Item2).Any(k => k <= 20);
                }
            }

            // Loop through the RSI values
            for (int i = 0; i < rsiValues.Count; i++)
            {
                // Get the RSI, MACD, and stochastic values for the current time period
                IPrice price = prices[i];
                decimal rsiValue = rsiValues[i];
                decimal macdValue = macdValues[i];
                decimal macdSignalValue = macdSignalValues[i];
                decimal stochasticKValue = kValues[i];
                decimal stochasticDValue = dValues[i];

                result.Add(new StockData
                {
                    Date = price.Date.DateTime,
                    Exchange = exchange,
                    MACD = macdValue,
                    Signal = macdSignalValue,
                    RSI = rsiValue,
                    StochasticK = stochasticKValue,
                    StochasticD = stochasticDValue,
                    Ticker = ticker,
                    PriceClose = price.Close,
                    Volume = price.Volume,
                    MACDCrossDirectionLast14Days = macdCrossCheck14,
                    RSICrossDirectionLast14Days = rsiCrossCheck14,
                    StochCrossDirectionLast14Days = stochCrossCheck14,
                    MACDCrossDirectionLast5Days = macdCrossCheck5,
                    RSICrossDirectionLast5Days = rsiCrossCheck5,
                    StochCrossDirectionLast5Days = stochCrossCheck5,
                    StochasticInOverboughtLast14Days = stochasticInOverboughtLast14Days,
                    StochasticInOverboughtLast5Days = stochasticInOverboughtLast5Days,
                    StochasticInOversoldLast14Days = stochasticInOversoldLast14Days,
                    StochasticInOversoldLast5Days = stochasticInOversoldLast5Days,
                    SqueezeMomentumV2 = squeezeMomentumV3,
                });
            }

            return result;
        }

        public static async Task<IEnumerable<Stock>> GetStocksFromUSCANExchanges(string apiKey)
        {
            List<string> exchanges = new List<string>() { "NYSE", "NasdaqNM", "AMEX", "TSX", "TSXV", "MX" };
            string baseUrl = "https://financialmodelingprep.com/api/v3";

            using (var httpClient = new HttpClient())
            {
                // Set the criteria for the search
                string url = $"{baseUrl}/stock-screener?volumeMoreThan=3000000&priceLowerThan=20&apikey={apiKey}";

                // Send the request and get the response
                var response = await httpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var stocks = JArray.Parse(responseString).ToObject<List<Stock>>();

                // Filter the results by exchange short name
                var filteredStocks = stocks.Where(s => exchanges.Any(ex => ex.ToLower().Contains(s.ExchangeShortName.ToLower()))).ToList();

                return filteredStocks;
            }
        }
    }
}
