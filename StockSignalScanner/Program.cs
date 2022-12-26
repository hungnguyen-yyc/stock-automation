using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YahooFinance.NET;

namespace TickerList
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                var northAmericaStocks = await GetStocksFromUSCANExchanges("e2b2a6d07ebf89ca33bb96b0b590daab");
                var random = new Random();
                var randomNumber = random.Next(1, northAmericaStocks.Count() - 1);
                var nowTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(@"C:\Users\hnguyen\Documents\stock-scan-logs", $"{nowTime}.csv"), true))
                {
                    outputFile.WriteLine("Time,Ticker,PriceClose,Volume,RSI,StochasticK,StochasticD,MACD,MACDSignal,ChatGPTIndicator,YoutubeIndicator");
                    foreach (var stock in northAmericaStocks.Skip(randomNumber).Take(100))
                    {
                        var data = await ApplyIndicators(stock.Symbol, "e2b2a6d07ebf89ca33bb96b0b590daab");
                        if (data != null)
                        {
                            var str = data.ToString();
                            outputFile.WriteLine(str);
                        }
                    }
                }
            }
        }

        private static async Task<StockData> ApplyIndicators(string ticker, string apiKey)
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

                // Get the RSI values
                decimal[] rsiValues = GetRSI(prices);
                decimal rsi = rsiValues.LastOrDefault();

                // Get the MACD values
                (decimal[] macdValues, decimal[] signalValues) = GetMACD(prices);
                decimal macd = macdValues.LastOrDefault();
                decimal signal = signalValues.LastOrDefault();

                // Get the Stochastic values
                (decimal[] kValues, decimal[] dValues) = GetStochastic(prices);
                decimal k = kValues.LastOrDefault();
                decimal d = dValues.LastOrDefault();

                return new StockData
                {
                    Date = DateTime.Now,
                    MACD = macd,
                    Signal = signal,
                    RSI = rsi,
                    StochasticK = k,
                    StochasticD = d,
                    Ticker = ticker,
                    PriceClose = prices.Historical?.Last()?.Close ?? 0,
                    Volume = prices.Historical?.Last()?.Volume ?? 0,
                };
            }
            return null;
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

        private static decimal[] GetRSI(HistoricalPrice prices)
        {
            // Get the weekly close prices
            int weeks = prices.Historical.Count / 5;
            decimal[] closePrices = new decimal[weeks];
            for (int i = 0; i < weeks; i++)
            {
                closePrices[i] = prices.Historical.Skip(i * 5).Take(5).Last().Close;
            }

            // Calculate the RSI values
            int period = 14;
            decimal[] rsiValues = new decimal[weeks];
            decimal[] avgGainValues = new decimal[weeks];
            decimal[] avgLossValues = new decimal[weeks];
            for (int i = 0; i < weeks; i++)
            {
                if (i > 0)
                {
                    decimal change = closePrices[i] - closePrices[i - 1];
                    if (change > 0)
                    {
                        avgGainValues[i] = avgGainValues[i - 1] * (period - 1) / period + change / period;
                        avgLossValues[i] = avgLossValues[i - 1] * (period - 1) / period;
                    }
                    else
                    {
                        avgGainValues[i] = avgGainValues[i - 1] * (period - 1) / period;
                        avgLossValues[i] = avgLossValues[i - 1] * (period - 1) / period - change / period;
                    }
                }

                if (i >= period)
                {
                    // Calculate the RSI value
                    rsiValues[i] = 100 - 100 / (1 + avgGainValues[i] / avgLossValues[i]);
                }
            }

            return rsiValues;
        }

        private static (decimal[], decimal[]) GetMACD(HistoricalPrice prices)
        {
            // Get the weekly close prices
            int weeks = prices.Historical.Count / 5;
            decimal[] closePrices = new decimal[weeks];
            for (int i = 0; i < weeks; i++)
            {
                closePrices[i] = prices.Historical.Skip(i * 5).Take(5).Last().Close;
            }

            // Calculate the MACD values
            int shortPeriod = 12;
            int longPeriod = 26;
            int signalPeriod = 9;
            decimal[] macdValues = new decimal[weeks];
            decimal[] signalValues = new decimal[weeks];
            for (int i = 0; i < weeks; i++)
            {
                if (i > 0)
                {
                    // Calculate the MACD value
                    macdValues[i] = closePrices.Take(i + 1).Reverse().Take(shortPeriod).Average() - closePrices.Take(i + 1).Reverse().Take(longPeriod).Average();
                }

                if (i >= longPeriod)
                {
                    // Calculate the signal value
                    signalValues[i] = macdValues.Take(i + 1).Reverse().Take(signalPeriod).Average();
                }
            }

            return (macdValues, signalValues);
        }

        //private static async Task<string> GetMACDCross(string ticker)
        //{
        //    // Get the MACD and signal values
        //    (decimal[] macdValues, decimal[] signalValues) = await GetMACD(ticker);

        //    // Find the most recent MACD and signal values
        //    decimal mostRecentMACD = macdValues.Last();
        //    decimal mostRecentSignal = signalValues.Last();

        //    // Find the second most recent MACD and signal values
        //    decimal secondMostRecentMACD = macdValues[macdValues.Length - 2];
        //    decimal secondMostRecentSignal = signalValues[signalValues.Length - 2];

        //    // Determine whether the MACD line has crossed above or below the signal line
        //    if (mostRecentMACD > mostRecentSignal && secondMostRecentMACD < secondMostRecentSignal)
        //    {
        //        return "crossed above";
        //    }
        //    else if (mostRecentMACD < mostRecentSignal && secondMostRecentMACD > secondMostRecentSignal)
        //    {
        //        return "crossed below";
        //    }
        //    else
        //    {
        //        return "no cross";
        //    }
        //}

        private static (decimal[], decimal[]) GetStochastic(HistoricalPrice prices)
        {
            // Get the weekly close prices, high prices, and low prices
            int weeks = prices.Historical.Count / 5;
            decimal[] closePrices = new decimal[weeks];
            decimal[] highPrices = new decimal[weeks];
            decimal[] lowPrices = new decimal[weeks];
            for (int i = 0; i < weeks; i++)
            {
                closePrices[i] = prices.Historical.Skip(i * 5).Take(5).Last().Close;
                highPrices[i] = prices.Historical.Skip(i * 5).Take(5).Max(p => p.High);
                lowPrices[i] = prices.Historical.Skip(i * 5).Take(5).Min(p => p.Low);
            }

            // Calculate the %K and %D values
            int period = 14;
            decimal[] kValues = new decimal[weeks];
            decimal[] dValues = new decimal[weeks];
            for (int i = 0; i < weeks; i++)
            {
                if (i >= period)
                {
                    if (highPrices.Take(i + 1).Max() - lowPrices.Take(i + 1).Min() != 0)
                    {
                        kValues[i] = (closePrices[i] - lowPrices.Take(i + 1).Min()) / (highPrices.Take(i + 1).Max() - lowPrices.Take(i + 1).Min()) * 100;
                    }
                    else
                    {
                        // Set the k value to zero in this case
                        kValues[i] = 0;
                    }
                }

                if (i > 0)
                {
                    // Calculate the %D value
                    dValues[i] = kValues.Take(i + 1).Average();
                }
            }

            // Return the %K and %D values
            return (kValues, dValues);
        }


    }

    public class StockData
    {
        public DateTime Date { get; set; }
        public string Ticker { get; set; }
        public decimal RSI { get; set; }
        public decimal StochasticK { get; set; }
        public decimal StochasticD { get; set; }
        public decimal MACD { get; set; }
        public decimal Signal { get; set; }
        public decimal PriceClose { get; set; }
        public decimal Volume { get; set; }
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
                    return TickerAction.HOLD;
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
                    return TickerAction.HOLD;
                }
            }
        }

        public override string ToString()
        {
            return $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")},{Ticker},{PriceClose},{Volume},{RSI},{StochasticK},{StochasticD},{MACD},{Signal},{ChatGPTIndicator},{YoutubeIndicator}";
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
        HOLD
    }
}
