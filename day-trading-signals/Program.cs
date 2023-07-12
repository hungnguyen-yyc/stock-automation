using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockSignalScanner.Models;
using Xunit.Sdk;

public class Program
{
    private static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";
    private static string PATH = @"C:\Users\hnguyen\Documents\stock-scan-logs-day-trade";
    private static int TIME_BETWEEN_PIVOTS = 60;
    private static int NUMBER_OF_5_MIN_CANDLES_IN_A_DAY = 79;
    private static int TOP_OR_BOTTOM_LIMIT = 30;
    private static int NUMBER_OF_SWING_POINTS = 5;

    public static async Task Main(string[] args)
    {
        var favs = new List<string>() { "AMD", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "MARA", "RIOT", "RBLX", "SPY" };
        using (var httpClient = new HttpClient())
        {
            foreach (var ticker in favs)
            {
                string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/technical_indicator/5min/{ticker}?period=50&type=tema&apikey={API_KEY}";

                var response = await httpClient.GetAsync(API_ENDPOINT);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var tickerHistoricalPrices = JsonConvert.DeserializeObject<IList<Price>>(content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (tickerHistoricalPrices == null )
                    {
                        return;
                    }
                    var resultByTopAndBottomMfis = DetectDivergenceByTopAndBottomMfis(ticker, tickerHistoricalPrices.Reverse().ToList());

                    var resultBySwingHighsLows = DetectDivergenceBySwingHighsOrLows(ticker, tickerHistoricalPrices.Reverse().ToList());

                    if (resultByTopAndBottomMfis.Any())
                    {
                        resultByTopAndBottomMfis = resultByTopAndBottomMfis.Reverse().ToList();
                        var todayPath = $@"{PATH}\{DateTime.Now.ToString("yyyy-MM-dd")}";
                        if (Directory.Exists(PATH) == false)
                        {
                            Directory.CreateDirectory(PATH);
                        }
                        if (Directory.Exists(todayPath) == false)
                        {
                            Directory.CreateDirectory(todayPath);
                        }
                        File.WriteAllText($@"{todayPath}\{ticker}-top-bottom-mfis.txt", string.Join("\n", resultByTopAndBottomMfis));
                    }

                    if (resultBySwingHighsLows.Any())
                    {
                        resultBySwingHighsLows = resultBySwingHighsLows.Reverse().ToList();
                        var todayPath = $@"{PATH}\{DateTime.Now.ToString("yyyy-MM-dd")}";
                        if (Directory.Exists(PATH) == false)
                        {
                            Directory.CreateDirectory(PATH);
                        }
                        if (Directory.Exists(todayPath) == false)
                        {
                            Directory.CreateDirectory(todayPath);
                        }
                        File.WriteAllText($@"{todayPath}\{ticker}-swing-highs-lows.txt", string.Join("\n", resultBySwingHighsLows));
                    }
                }
            }
        }
    }

    public static IList<string> DetectDivergenceBySwingHighsOrLows(string ticker, IList<Price> prices)
    {
        var result = new List<string>();
        var swingHighs = FindSwingHighs(prices.ToList<Price>(), 5);
        var swingLows = FindSwingLows(prices.ToList<Price>(), 5);
        var mfis = prices.GetMfi(14).ToList();
        var macds = prices.GetMacd(12, 26, 9).ToList();

        var swingHighsLast1Days = swingHighs.Skip(swingHighs.Count - NUMBER_OF_SWING_POINTS).Take(NUMBER_OF_SWING_POINTS).ToList();
        var swingLowsLast1Days = swingLows.Skip(swingLows.Count - NUMBER_OF_SWING_POINTS).Take(NUMBER_OF_SWING_POINTS).ToList();

        var startDateMap = new Dictionary<DateTime, string>();
        
        for (int i = 0; i < swingHighsLast1Days.Count; i++)
        {
            var price1 = swingHighsLast1Days[i];
            var price1Day = price1.Date.Day;
            var price1Hour = price1.Date.Hour;
            var price1Minute = price1.Date.Minute;

            for (int j = i + 1; j < swingHighsLast1Days.Count; j++)
            {
                var price2 = swingHighsLast1Days[j];
                var price2Day = price2.Date.Day;
                var price2Hour = price2.Date.Hour;
                var price2Minute = price2.Date.Minute;

                var goodToCheck = false; 
                if (price1Day == price2Day)
                {
                    var timeDiff = price1Hour * 60 + price1Minute - (price2Hour * 60 + price2Minute);
                    goodToCheck = Math.Abs(timeDiff) >= TIME_BETWEEN_PIVOTS;
                }
                else
                {
                    if (price1Hour == 15 || price2Hour == 15)
                    {
                        goodToCheck = false;
                    }
                    else
                    {
                        goodToCheck = true;
                    }
                }

                if (goodToCheck)
                {
                    var mfiResult1 = mfis.FirstOrDefault(m => m.Date == price1.Date);
                    var mfiResult2 = mfis.FirstOrDefault(m => m.Date == price2.Date);
                    var macd = macds.FirstOrDefault(m => m.Date == price2.Date);

                    var macdString = "";
                    var temaString = "";
                    var resultString = "";
                    if (macd != null && macd.Macd < macd.Signal)
                    {
                        macdString = $"*** MACD ({macd.Macd.Value.ToString("0.##")}) < Signal ({macd.Signal.Value.ToString("0.##")}) ***";
                    }
                    if (price2.Close <= price2.Tema)
                    {
                        temaString = $"*** Close ({price2.Close}) <= Tema ({price2.Tema}) ***";
                    }

                    if (mfiResult1 == null || mfiResult2 == null)
                    {
                        continue;
                    }

                    if (mfiResult1.Mfi > mfiResult2.Mfi && price1.High <= price2.High)
                    {
                        resultString = $"- {ticker}: Regular Bearish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }
                    if (mfiResult1.Mfi < mfiResult2.Mfi && price1.High >= price2.High)
                    {
                        resultString = $"- {ticker}: Hidden Bearish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }

                    if (!string.IsNullOrEmpty(resultString))
                    {
                        if (!startDateMap.ContainsKey(price1.Date))
                        {
                            startDateMap.Add(price1.Date, resultString);
                        }
                        else
                        {
                            startDateMap[price1.Date] = resultString;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < swingLowsLast1Days.Count; i++)
        {
            var price1 = swingLowsLast1Days[i];
            var price1Day = price1.Date.Day;
            var price1Hour = price1.Date.Hour;
            var price1Minute = price1.Date.Minute;

            for (int j = i + 1; j < swingLowsLast1Days.Count; j++)
            {
                var price2 = swingLowsLast1Days[j];
                var price2Day = price2.Date.Day;
                var price2Hour = price2.Date.Hour;
                var price2Minute = price2.Date.Minute;

                var goodToCheck = false;
                if (price1Day == price2Day)
                {
                    var timeDiff = price1Hour * 60 + price1Minute - (price2Hour * 60 + price2Minute);
                    goodToCheck = Math.Abs(timeDiff) >= TIME_BETWEEN_PIVOTS;
                }
                else
                {
                    if (price1Hour == 15 || price2Hour == 15)
                    {
                        goodToCheck = false;
                    }
                    else
                    {
                        goodToCheck = true;
                    }
                }

                if (goodToCheck)
                {
                    var mfiResult1 = mfis.FirstOrDefault(m => m.Date == price1.Date);
                    var mfiResult2 = mfis.FirstOrDefault(m => m.Date == price2.Date);
                    var macd = macds.FirstOrDefault(m => m.Date == price2.Date);

                    var macdString = "";
                    var temaString = "";
                    var resultString = "";
                    if (macd != null && macd.Macd < macd.Signal)
                    {
                        macdString = $"*** MACD ({macd.Macd.Value.ToString("0.##")}) < Signal ({macd.Signal.Value.ToString("0.##")}) ***";
                    }
                    if (price2.Close <= price2.Tema)
                    {
                        temaString = $"*** Close ({price2.Close}) <= Tema ({price2.Tema}) ***";
                    }

                    if (mfiResult1 == null || mfiResult2 == null)
                    {
                        continue;
                    }

                    if (mfiResult1.Mfi < mfiResult2.Mfi && price1.Low >= price2.Low)
                    {
                        resultString = $"- {ticker}: Regular Bullish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }
                    if (mfiResult1.Mfi > mfiResult2.Mfi && price1.Low <= price2.Low)
                    {
                        resultString = $"- {ticker}: Hidden Bullish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }

                    if (!string.IsNullOrEmpty(resultString))
                    {
                        if (!startDateMap.ContainsKey(price1.Date))
                        {
                            startDateMap.Add(price1.Date, resultString);
                        }
                        else
                        {
                            startDateMap[price1.Date] = resultString;
                        }
                    }
                }
            }
        }

        foreach (var item in startDateMap)
        {
            result.Add(item.Value);
        }
        return result;
    }

    public static IList<string> DetectDivergenceByTopAndBottomMfis(string ticker, IList<Price> prices)
    {
        var result = new List<string>();
        var mfis = prices.GetMfi(14).ToList();
        var macds = prices.GetMacd(12, 26, 9).ToList();
        var mfisLast2Days = mfis.Skip(mfis.Count - NUMBER_OF_5_MIN_CANDLES_IN_A_DAY).Take(NUMBER_OF_5_MIN_CANDLES_IN_A_DAY).ToList();

        var highestMfisOrderByDate = mfisLast2Days.OrderByDescending(m => m.Mfi).Take(TOP_OR_BOTTOM_LIMIT).OrderBy(m => m.Date).ToList();
        var pricesAtMfiDate = prices.Where(p => highestMfisOrderByDate.Any(m => m.Date == p.Date)).OrderBy(m => m.Date).ToList();
        var macdsAtMfiDate = macds.Where(p => highestMfisOrderByDate.Any(m => m.Date == p.Date)).OrderBy(m => m.Date).ToList();

        var startDateMap = new Dictionary<DateTime, string>();

        for (int i = 0; i < highestMfisOrderByDate.Count; i++)
        {
            var mfiResult1 = highestMfisOrderByDate[i];
            var price1 = pricesAtMfiDate[i];
            var price1Day = price1.Date.Day;
            var price1Hour = price1.Date.Hour;
            var price1Minute = price1.Date.Minute;

            for (int j = i + 1; j < highestMfisOrderByDate.Count; j++)
            {
                var mfiResult2 = highestMfisOrderByDate[j];
                var price2 = pricesAtMfiDate[j];
                var price2Day = price2.Date.Day;
                var price2Hour = price2.Date.Hour;
                var price2Minute = price2.Date.Minute;

                var goodToCheck = false;
                if (price1Day == price2Day)
                {
                    var timeDiff = price1Hour * 60 + price1Minute - (price2Hour * 60 + price2Minute);
                    goodToCheck = Math.Abs(timeDiff) >= TIME_BETWEEN_PIVOTS;
                } 
                else
                {
                    if (price1Hour == 15 || price2Hour == 15)
                    {
                        goodToCheck = false;
                    }
                    else
                    {
                        goodToCheck = true;
                    }
                }

                if (goodToCheck)
                {
                    var macd = macdsAtMfiDate[j];
                    var macdString = "";
                    var temaString = "";
                    var resultString = "";
                    if (macd.Macd < macd.Signal)
                    {
                        macdString = $"*** MACD ({macd.Macd.Value.ToString("0.##")}) < Signal ({macd.Signal.Value.ToString("0.##")}) ***";
                    }
                    if (price2.Close <= price2.Tema)
                    {
                        temaString = $"*** Close ({price2.Close}) <= Tema ({price2.Tema}) ***";
                    }

                    if (mfiResult1.Mfi > mfiResult2.Mfi && price1.High <= price2.High)
                    {
                        resultString = $"- {ticker}: Regular Bearish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }
                    if (mfiResult1.Mfi < mfiResult2.Mfi && price1.High >= price2.High)
                    {
                        resultString = $"- {ticker}: Hidden Bearish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }

                    if (!string.IsNullOrEmpty(resultString))
                    {
                        if (!startDateMap.ContainsKey(price1.Date))
                        {
                            startDateMap.Add(price1.Date, resultString);
                        }
                        else
                        {
                            startDateMap[price1.Date] = resultString;
                        }
                    }
                }

            }
        }

        var lowestMfisOrderByDate = mfisLast2Days.OrderBy(m => m.Mfi).Take(TOP_OR_BOTTOM_LIMIT).OrderBy(m => m.Date).ToList();
        pricesAtMfiDate = prices.Where(p => lowestMfisOrderByDate.Any(m => m.Date == p.Date)).OrderBy(m => m.Date).ToList();

        for (int i = 0; i < lowestMfisOrderByDate.Count; i++)
        {
            var mfiResult1 = lowestMfisOrderByDate[i];
            var price1 = pricesAtMfiDate[i];
            var price1Day = price1.Date.Day;
            var price1Hour = price1.Date.Hour;
            var price1Minute = price1.Date.Minute;

            for (int j = i + 1; j < lowestMfisOrderByDate.Count; j++)
            {
                var mfiResult2 = lowestMfisOrderByDate[j];
                var price2 = pricesAtMfiDate[j];
                var price2Day = price2.Date.Day;
                var price2Hour = price2.Date.Hour;
                var price2Minute = price2.Date.Minute;

                var goodToCheck = false;

                if (price1Day == price2Day)
                {
                    var timeDiff = price1Hour * 60 + price1Minute - (price2Hour * 60 + price2Minute);
                    goodToCheck = Math.Abs(timeDiff) >= TIME_BETWEEN_PIVOTS;
                }
                else
                {
                    if (price1Hour == 15 || price2Hour == 15)
                    {
                        goodToCheck = false;
                    }
                    else
                    {
                        goodToCheck = true;
                    }
                }

                if (goodToCheck)
                {
                    var macd = macdsAtMfiDate[j];
                    var macdString = "";
                    var temaString = "";
                    var resultString = "";
                    if (macd.Macd > macd.Signal)
                    {
                        macdString = $"*** MACD ({macd.Macd.Value.ToString("0.##")}) > Signal ({macd.Signal.Value.ToString("0.##")}) ***";
                    }
                    if (price2.Close >= price2.Tema)
                    {
                        temaString = $"*** Close ({price2.Close}) >= Tema ({price2.Tema}) ***";
                    }

                    if (mfiResult1.Mfi < mfiResult2.Mfi && price1.Low >= price2.Low)
                    {
                        resultString = $"- {ticker}: Regular Bullish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }
                    if (mfiResult1.Mfi > mfiResult2.Mfi && price1.Low <= price2.Low)
                    {
                        resultString = $"- {ticker}: Hidden Bullish Divergence Found for at {price1.Date} and {price2.Date} | {macdString} | {temaString}";
                    }

                    if (!string.IsNullOrEmpty(resultString))
                    {
                        if (!startDateMap.ContainsKey(price1.Date))
                        {
                            startDateMap.Add(price1.Date, resultString);
                        }
                        else
                        {
                            startDateMap[price1.Date] = resultString;
                        }
                    }
                }

            }
        }

        foreach (var item in startDateMap)
        {
            result.Add(item.Value);
        }
        return result;
    }

    private static List<Price> FindSwingLows(List<Price> prices, int range)
    {
        List<Price> swingLows = new List<Price>();

        for (int i = range; i < prices.Count - range; i++)
        {
            var currentPrice = prices[i];

            bool isSwingLow = true;

            for (int j = i - range; j <= i + range; j++)
            {
                if (j == i)
                    continue;

                var price = prices[j];

                if (price.Low <= currentPrice.Low)
                {
                    isSwingLow = false;
                    break;
                }
            }

            if (isSwingLow)
            {
                swingLows.Add(currentPrice);
            }
        }

        return swingLows;
    }


    private static List<Price> FindSwingHighs(List<Price> prices, int range)
    {
        var swingHighs = new List<Price>();

        for (int i = range; i < prices.Count - range; i++)
        {
            var currentPrice = prices[i];

            bool isSwingHigh = true;

            for (int j = i - range; j <= i + range; j++)
            {
                if (j == i)
                    continue;

                var price = prices[j];

                if (price.High >= currentPrice.High)
                {
                    isSwingHigh = false;
                    break;
                }
            }

            if (isSwingHigh)
            {
                swingHighs.Add(currentPrice);
            }
        }

        return swingHighs;
    }


    private enum SignalState
    {
        HiddenBullishDivergenceFound,
        RegularBullishDivergenceFound,
        HiddenBearishDivergenceFound,
        RegularBearishDivergenceFound,
        SignalConfirm,
        Reset,
    }
}