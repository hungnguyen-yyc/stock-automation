using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YahooFinance.NET;

namespace TickerList
{
    public class Program
    {
        static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static List<Stock> POPULAR_STOCKS = new List<Stock>
        {
            new Stock
            {
                Symbol = "AMC",
                Name = "AMC Entertainment Holdings, Inc.",
                Price = 4.4m,
                Exchange = "New York Stock Exchange",
                ExchangeShortName = "NYSE",
                Type = "stock"
            },
            new Stock
            {
                Symbol = "GME",
                Name = "GameStop Corp.",
                Price = 20.08m,
                Exchange = "New York Stock Exchange",
                ExchangeShortName = "NYSE",
                Type = "stock"
            },
            new Stock
            {
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Price = 131.86m,
                Exchange = "NASDAQ Global Select",
                ExchangeShortName = "NASDAQ",
                Type = "stock"
            },
            new Stock
            {
                Symbol = "TSLA",
                Name = "Tesla, Inc.",
                Price = 123.15m,
                Exchange = "NASDAQ Global Select",
                ExchangeShortName = "NASDAQ",
                Type = "stock"
            },
            new Stock
            {
                Symbol = "AMZN",
                Name = "Amazon.com, Inc.",
                Price = 85.25m,
                Exchange = "NASDAQ Global Select",
                ExchangeShortName = "NASDAQ",
                Type = "stock"
            },
            new Stock
            {
                Symbol = "META",
                Name = "Meta Platforms, Inc.",
                Price = 118.04m,
                Exchange = "NASDAQ Global Select",
                ExchangeShortName = "NASDAQ",
                Type = "stock"
            },
            new Stock
            {
                Symbol = "MARA",
                Name = "Marathon Digital Holdings, Inc.",
                Price = 3.62m,
                Exchange = "NASDAQ Capital Market",
                ExchangeShortName = "NASDAQ",
                Type = "stock"
            },
        };

        public static async Task Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                var northAmericaStocks = POPULAR_STOCKS; //await GetStocksFromUSCANExchanges(API_KEY); // update to get correct exchanges
                
                var random = new Random();
                var randomNumber = random.Next(1, northAmericaStocks.Count() - 1);
                var nowTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
                var folderPath = @"C:\Users\hnguyen\Documents\stock-scan-logs";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                var scanFolderPath = Path.Combine(folderPath, nowDate);
                if (!Directory.Exists(scanFolderPath))
                {
                    Directory.CreateDirectory(scanFolderPath);
                }
                foreach (var stock in northAmericaStocks.Skip(randomNumber))
                {
                    try
                    {
                        var tickerActionString = "";
                        var fileName = Path.Combine(scanFolderPath, $"{stock.Symbol}-{nowTime}.csv");
                        using (StreamWriter outputFile = new StreamWriter(fileName, true))
                        {
                            outputFile.WriteLine("Time,Ticker,Exchange,PriceClose,Volume,RSI,StochasticK,StochasticD,MACD,MACDSignal,RSICheck,StochCheck,MACDCheck");
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
                                    outputFile.WriteLine(datum.ToString());
                                }
                            }
                        }
                        File.Move(fileName, Path.Combine(scanFolderPath, $"{stock.Symbol}-{nowTime}-{tickerActionString}.csv"));
                    }
                    catch (Exception ex)
                    {
                        using (StreamWriter outputFile = new StreamWriter(Path.Combine(scanFolderPath, $"error-{stock.Symbol}-{nowTime}.txt"), true))
                        {
                            outputFile.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
        }

        private static async Task<List<StockData>> RunScan(string ticker, string exchange, string apiKey)
        {
            string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?apikey={apiKey}";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(API_ENDPOINT);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                HistoricalPrice prices = JsonConvert.DeserializeObject<HistoricalPrice>(content);

                if (prices == null || prices.Historical == null)
                {
                    return null;
                }

                var reverse = prices.Historical.OrderBy(p => p.Date).ToList();
                return GetIndicators(ticker, exchange, reverse, 14, 12, 26, 9, 14);
            }
            return null;
        }

        public static List<StockData> GetIndicators(string ticker, string exchange, List<Price> prices, int rsiPeriod, int macdShortPeriod, int macdLongPeriod, int macdSignalPeriod, int stochasticPeriod)
        {
            var result = new List<StockData>();

            // Get the RSI, MACD, and stochastic values and times
            (List<decimal> rsiValues, List<DateTime> rsiTimes) = GetRSI(prices, rsiPeriod);
            (List<decimal> macdValues, List<decimal> macdSignalValues, List<DateTime> macdTimes) = GetMACD(prices, macdShortPeriod, macdLongPeriod, macdSignalPeriod);
            (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) = GetStochastic(prices, stochasticPeriod);

            // Loop through the RSI values
            for (int i = 0; i < rsiValues.Count; i++)
            {
                // Get the RSI, MACD, and stochastic values for the current time period
                Price price = prices[i];
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
                string url = $"{baseUrl}/stock/list?apikey={apiKey}";

                // Send the request and get the response
                var response = await httpClient.GetAsync(url);
                var responseString = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var stocks = JArray.Parse(responseString).ToObject<List<Stock>>();

                // Filter the results by exchange short name
                var filteredStocks = stocks.Where(s => exchanges.Contains(s.ExchangeShortName)).ToList();

                return filteredStocks;
            }
        }

        private static (List<decimal> rsiValues, List<DateTime> rsiTimes) GetRSI(List<Price> prices, int period)
        {
            // Initialize lists to store the RSI and time values
            List<decimal> rsiValues = new List<decimal>();
            List<DateTime> rsiTimes = new List<DateTime>();

            // Extract the close prices and times from the Price objects
            List<decimal> closePrices = prices.Select(p => p.Close).ToList();
            List<DateTime> times = prices.Select(p => p.Date.DateTime).ToList();

            // Initialize variables to store the average gain and average loss
            decimal avgGain = 0;
            decimal avgLoss = 0;

            // Calculate the RSI values using a sliding window
            for (int i = 0; i < closePrices.Count; i++)
            {
                // Check if we have enough data to calculate the RSI value
                if (i >= period)
                {
                    // Calculate the change in price from the previous period
                    decimal change = closePrices[i] - closePrices[i - 1];

                    // Update the average gain and average loss
                    if (change > 0)
                    {
                        avgGain = (avgGain * (period - 1) + change) / period;
                        avgLoss = avgLoss * (period - 1) / period;
                    }
                    else
                    {
                        avgGain = avgGain * (period - 1) / period;
                        avgLoss = (avgLoss * (period - 1) - change) / period;
                    }

                    // Calculate the RSI value
                    rsiValues.Add(avgLoss == 0 ? 100 : 100 - (100 / (1 + (avgGain / avgLoss))));
                }
                else
                {
                    // Set the RSI value to zero until we have enough data
                    rsiValues.Add(0);
                }

                // Add the time value
                rsiTimes.Add(times[i]);
            }

            // Return the RSI and time values
            return (rsiValues, rsiTimes);
        }


        public static (List<decimal> macdValues, List<decimal> signalValues, List<DateTime> macdTimes) GetMACD(List<Price> prices, int shortPeriod, int longPeriod, int signalPeriod)
        {
            // Initialize lists to store the MACD, signal, and time values
            List<decimal> macdValues = new List<decimal>();
            List<decimal> signalValues = new List<decimal>();
            List<DateTime> macdTimes = new List<DateTime>();

            // Extract the close prices and times from the Price objects
            List<decimal> closePrices = prices.Select(p => p.Close).ToList();
            List<DateTime> times = prices.Select(p => p.Date.DateTime).ToList();

            // Calculate the MACD value
            List<decimal> shortEMA = pineEMA(closePrices.ToList(), shortPeriod);
            List<decimal> longEMA = pineEMA(closePrices.ToList(), longPeriod);

            // Calculate the MACD and signal values
            for (int i = 0; i < closePrices.Count; i++)
            {
                macdValues.Add(shortEMA[i] - longEMA[i]);

                // Add the time value
                macdTimes.Add(times[i]);
            }

            signalValues.AddRange(pineEMA(macdValues, signalPeriod));

            // Return the MACD, signal, and time values
            return (macdValues, signalValues, macdTimes);
        }


        private static List<decimal> pineEMA(List<decimal> src, int length)
        {
            decimal alpha = 2.0m / (length + 1);
            List<decimal> sum = new List<decimal>();

            for (int i = 0; i < src.Count; i++)
            {
                decimal previousSum = (i == 0 ? 0 : sum[i - 1]);
                sum.Add(alpha * src[i] + (1 - alpha) * previousSum);
            }

            return sum;
        }

        public static (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) GetStochastic(List<Price> prices, int period)
        {
            // Initialize lists to store the K, D, and time values
            List<decimal> kValues = new List<decimal>();
            List<decimal> dValues = new List<decimal>();
            List<DateTime> stochasticTimes = new List<DateTime>();

            // Extract the close, high, and low prices and times from the Price objects
            List<decimal> closePrices = prices.Select(p => p.Close).ToList();
            List<decimal> highPrices = prices.Select(p => p.High).ToList();
            List<decimal> lowPrices = prices.Select(p => p.Low).ToList();
            List<DateTime> times = prices.Select(p => p.Date.DateTime).ToList();

            // Calculate the K and D values
            for (int i = 0; i < closePrices.Count; i++)
            {
                // Check if we have enough data to calculate the K value
                if (i >= period - 1)
                {
                    // Calculate the minimum and maximum prices over the previous period
                    var closePrice = closePrices[i];
                    decimal minPrice = lowPrices.Skip(i - period + 1).Take(period).Min();
                    decimal maxPrice = highPrices.Skip(i - period + 1).Take(period).Max();
                    var k = 100 * (closePrice - minPrice) / (maxPrice - minPrice);
                    // Calculate the K value
                    kValues.Add(k);
                }
                else
                {
                    // Set the K and D values to zero until we have enough data
                    kValues.Add(0);
                }

                // Calculate the D value
                if (i >= period + 2)
                {
                    // Calculate the 3-day simple moving average of the K values
                    var d = kValues.Skip(i - 2).Take(3).Average();
                    dValues.Add(d);
                }
                else
                {
                    dValues.Add(0);
                }

                // Add the time value
                stochasticTimes.Add(times[i]);
            }

            // Return the K, D, and time values
            return (kValues, dValues, stochasticTimes);
        }

    }

    public class StockData
    {
        public DateTime Date { get; set; }
        public string Ticker { get; set; }
        public string Exchange { get; set; }
        public decimal RSI { get; set; }
        public decimal StochasticK { get; set; }
        public decimal StochasticD { get; set; }
        public decimal MACD { get; set; }
        public decimal Signal { get; set; }
        public decimal PriceClose { get; set; }
        public decimal Volume { get; set; }
        public TickerAction MACDCheck => MACD > Signal ? TickerAction.BUY : TickerAction.SELL;
        public TickerAction RSICheck => RSI > 50 ? TickerAction.BUY : TickerAction.SELL;
        public TickerAction StochCheck
        {
            get
            {
                if (StochasticD >= 80 && StochasticK >= 80)
                {
                    return TickerAction.SELL;
                }

                if (StochasticD <= 20 && StochasticK <= 20)
                {
                    return TickerAction.BUY;
                }

                return TickerAction.NO_ACTION;
            }
        }
        public TickerAction ChatGPTIndicator { 
            get
            {
                if (RSI < 30 && MACD > Signal && StochasticK < 20 && StochasticD < 20)
                {
                    return TickerAction.BUY;
                }
                else if (RSI > 70 && MACD < Signal && StochasticK > 80 && StochasticD > 80)
                {
                    return TickerAction.SELL;
                }
                else
                {
                    return TickerAction.NO_ACTION;
                }
            }
        }
        // https://www.youtube.com/watch?v=R1cKTKV6-gc
        public TickerAction YoutubeIndicator {
            get
            {
                if (RSI > 50 && MACD > Signal && StochasticK < 20 && StochasticD < 20)
                {
                    return TickerAction.BUY;
                }
                else if (RSI < 50 && MACD < Signal && StochasticK > 80 && StochasticD > 80)
                {
                    return TickerAction.SELL;
                }
                else
                {
                    return TickerAction.NO_ACTION;
                }
            }
        }

        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd-HH-mm-ss")},{Ticker},{Exchange},{PriceClose},{Volume},{RSI},{StochasticK},{StochasticD},{MACD},{Signal},{RSICheck},{StochCheck},{MACDCheck}";
        }

        public string GetRecommendTickerAction()
        {
            return $"{RSICheck}-{StochCheck}-{MACDCheck}";
        }
    }

    public class Stock
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Exchange { get; set; }
        public string ExchangeShortName { get; set; }
        public string Type { get; set; }
    }


    public class HistoricalPrice
    {
        public string Ticker { get; set; }
        public IList<Price> Historical { get; set; }
    }

    public class Price
    {
        public DateTimeOffset Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal AdjClose { get; set; }
        public long Volume { get; set; }
        public long UnadjustedVolume { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public decimal Vwap { get; set; }
        public string Label { get; set; }
        public decimal ChangeOverTime { get; set; }
    }

    public enum MACDStatus
    {
        MACD_ABOVE_SIGNAL,
        MACD_UNDER_SIGNAL
    }

    public enum TickerAction
    {
        BUY,
        SELL,
        NO_ACTION
    }
}
