﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection.PortableExecutable;
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
        private static readonly int[] CROSSES_IN_LAST_DAYS = new int[] { 14, 5 };

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
                                        var crossAbove14 = datum.MACDCrossDirectionLast14Days == CrossDirection.CROSS_ABOVE
                                            && datum.RSICrossDirectionLast14Days == CrossDirection.CROSS_ABOVE
                                            && datum.StochCrossDirectionLast14Days == CrossDirection.CROSS_ABOVE;
                                        var crossBelow14 = datum.MACDCrossDirectionLast14Days == CrossDirection.CROSS_BELOW
                                            && datum.RSICrossDirectionLast14Days == CrossDirection.CROSS_BELOW
                                            && datum.StochCrossDirectionLast14Days == CrossDirection.CROSS_BELOW;
                                        var crossAbove5 = datum.MACDCrossDirectionLast5Days == CrossDirection.CROSS_ABOVE
                                            && datum.RSICrossDirectionLast5Days == CrossDirection.CROSS_ABOVE
                                            && datum.StochCrossDirectionLast5Days == CrossDirection.CROSS_ABOVE;
                                        var crossBelow5 = datum.MACDCrossDirectionLast5Days == CrossDirection.CROSS_BELOW
                                            && datum.RSICrossDirectionLast5Days == CrossDirection.CROSS_BELOW
                                            && datum.StochCrossDirectionLast5Days == CrossDirection.CROSS_BELOW;
                                        allIndicatorCrossed5 = crossAbove5 || crossBelow5;
                                        allIndicatorCrossed14 = crossAbove14 || crossBelow14;
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

        private static void CreateScanDirectories(string folderPath, string scanFolderPath, string last14DaysCrossFolder, string last5DaysCrossFolder)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (!Directory.Exists(scanFolderPath))
            {
                Directory.CreateDirectory(scanFolderPath);
            }
            if (!Directory.Exists(last14DaysCrossFolder))
            {
                Directory.CreateDirectory(last14DaysCrossFolder);
            }
            if (!Directory.Exists(last5DaysCrossFolder))
            {
                Directory.CreateDirectory(last5DaysCrossFolder);
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
            CrossDirection macdCrossCheck14 = CrossDirection.NO_CROSS;
            CrossDirection stochCrossCheck14 = CrossDirection.NO_CROSS;
            CrossDirection rsiCrossCheck14 = CrossDirection.NO_CROSS;
            CrossDirection macdCrossCheck5 = CrossDirection.NO_CROSS;
            CrossDirection stochCrossCheck5 = CrossDirection.NO_CROSS;
            CrossDirection rsiCrossCheck5 = CrossDirection.NO_CROSS;

            foreach (var days in CROSSES_IN_LAST_DAYS)
            {
                List<(DateTime, decimal)> macdLine = macdTimes.Zip(macdValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> signalLine = macdTimes.Zip(macdSignalValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> kLine = stochasticTimes.Zip(kValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> dLine = stochasticTimes.Zip(dValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> rsiLine = rsiTimes.Zip(rsiValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> rsi50Line = rsiTimes.Select(r => (r, 50m)).Take(days).ToList();

                // base on this https://www.youtube.com/watch?v=R1cKTKV6-gc
                macdCrossCheck14 = GetCrossDirection(macdLine, signalLine);
                stochCrossCheck14 = GetCrossDirection(kLine, dLine);
                rsiCrossCheck14 = GetCrossDirection(rsiLine, rsi50Line);
                macdCrossCheck5 = GetCrossDirection(macdLine, signalLine);
                stochCrossCheck5 = GetCrossDirection(kLine, dLine);
                rsiCrossCheck5 = GetCrossDirection(rsiLine, rsi50Line);
            }

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
                    MACDCrossDirectionLast14Days = macdCrossCheck14,
                    RSICrossDirectionLast14Days = rsiCrossCheck14,
                    StochCrossDirectionLast14Days = stochCrossCheck14,
                    MACDCrossDirectionLast5Days = macdCrossCheck5,
                    RSICrossDirectionLast5Days = rsiCrossCheck5,
                    StochCrossDirectionLast5Days = stochCrossCheck5,
                });
            }

            return result;
        }

        public static async Task<IEnumerable<Stock>> GetStocksFromUSCANExchanges(string apiKey)
        {
            List<string> exchanges = new List<string>() { "NYSE", "NasdaqNM", "AMEX", "TSX", "TSXV", "MX" };
            string baseUrl = "https://financialmodelingprep.com/api/v3/stock-screener?volumeMoreThan=5000000&";

            using (var httpClient = new HttpClient())
            {
                // Set the criteria for the search
                string url = $"{baseUrl}/stock-screener?volumeMoreThan=5000000&apikey={apiKey}";

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
            List<decimal> shortEMA = CalculateEMA(closePrices.ToList(), shortPeriod);
            List<decimal> longEMA = CalculateEMA(closePrices.ToList(), longPeriod);

            // Calculate the MACD and signal values
            for (int i = 0; i < closePrices.Count; i++)
            {
                macdValues.Add(shortEMA[i] - longEMA[i]);

                // Add the time value
                macdTimes.Add(times[i]);
            }

            signalValues.AddRange(CalculateEMA(macdValues, signalPeriod));

            // Return the MACD, signal, and time values
            return (macdValues, signalValues, macdTimes);
        }


        private static List<decimal> CalculateEMA(List<decimal> src, int length)
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

                    if (minPrice == maxPrice)
                    {
                        kValues.Add(0);
                    } else
                    {
                        var k = 100 * (closePrice - minPrice) / (maxPrice - minPrice);
                        // Calculate the K value
                        kValues.Add(k);
                    }
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

        private static CrossDirection GetCrossDirection(List<(DateTime, decimal)> line1, List<(DateTime, decimal)> line2)
        {
            // Check that the lists have at least two elements each
            if (line1.Count < 2 || line2.Count < 2)
            {
                return CrossDirection.NO_CROSS;
            }

            // Initialize variables to track the previous values of line1 and line2
            decimal prevLine1Value = line1[0].Item2;
            decimal prevLine2Value = line2[0].Item2;

            // Initialize a variable to track the cross direction
            CrossDirection crossDirection = CrossDirection.NO_CROSS;

            // Iterate through the rest of the elements in the lists
            for (int i = 1; i < line1.Count; i++)
            {
                // Get the current values of line1 and line2
                decimal currLine1Value = line1[i].Item2;
                decimal currLine2Value = line2[i].Item2;

                // Check if line1 crossed above line2
                if (prevLine1Value < prevLine2Value && currLine1Value > currLine2Value)
                {
                    crossDirection = CrossDirection.CROSS_ABOVE;
                }

                // Check if line1 crossed below line2
                if (prevLine1Value > prevLine2Value && currLine1Value < currLine2Value)
                {
                    crossDirection = CrossDirection.CROSS_BELOW;
                }

                // Update the previous values of line1 and line2 for the next iteration
                prevLine1Value = currLine1Value;
                prevLine2Value = currLine2Value;
            }

            // Return the cross direction
            return crossDirection;
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
        public CrossDirection RSICrossDirectionLast14Days { get; set; }
        public CrossDirection MACDCrossDirectionLast14Days { get; set; }
        public CrossDirection StochCrossDirectionLast14Days { get; set; }
        public CrossDirection RSICrossDirectionLast5Days { get; set; }
        public CrossDirection MACDCrossDirectionLast5Days { get; set; }
        public CrossDirection StochCrossDirectionLast5Days { get; set; }
        public TickerAction MACDStatus => MACD > Signal ? TickerAction.OVERSOLD : TickerAction.OVERBOUGHT;
        public TickerAction RSIStatus
        {
            get
            {
                if(RSI <= 30)
                {
                    return TickerAction.OVERSOLD;
                } 
                else if (RSI >= 70)
                {
                    return TickerAction.OVERBOUGHT;
                }
                return TickerAction.MIXED;
            }
        }
        public TickerAction StochStatus
        {
            get
            {
                if (StochasticD >= 80 && StochasticK >= 80)
                {
                    return TickerAction.OVERBOUGHT;
                }

                if (StochasticD <= 20 && StochasticK <= 20)
                {
                    return TickerAction.OVERSOLD;
                }

                return TickerAction.MIXED;
            }
        }

        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd-HH-mm-ss")},{Ticker},{Exchange},{PriceClose},{Volume},{RSI},{StochasticK},{StochasticD},{MACD},{Signal},{RSIStatus},{StochStatus},{MACDStatus},{RSICrossDirectionLast14Days},{StochCrossDirectionLast14Days},{MACDCrossDirectionLast14Days}";
        }

        public string GetRecommendTickerAction()
        {
            return $"RSI_{RSICrossDirectionLast14Days}_MACD_{MACDCrossDirectionLast14Days}_{StochCrossDirectionLast14Days}_{StochStatus}_{Math.Round(StochasticK, 2)}_{Math.Round(StochasticD, 2)}";
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

    public enum CrossDirection 
    { 
        CROSS_ABOVE, 
        CROSS_BELOW, 
        NO_CROSS 
    }

    public enum TickerAction
    {
        OVERSOLD,
        OVERBOUGHT,
        MIXED
    }
}
