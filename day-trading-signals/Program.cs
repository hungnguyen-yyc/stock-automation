using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockSignalScanner.Models;
using Xunit.Sdk;

public class Program
{
    private static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";
    private static string PATH = @"C:\Users\hnguyen\Documents\stock-scan-logs-day-trade";

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
                    var result = DetectDivergenceByTopAndBottomMfis(ticker, tickerHistoricalPrices.Reverse().ToList());

                    if (result.Any())
                    {
                        result = result.Reverse().ToList();
                        var todayPath = $@"{PATH}\{DateTime.Now.ToString("yyyy-MM-dd")}";
                        if (Directory.Exists(PATH) == false)
                        {
                            Directory.CreateDirectory(PATH);
                        }
                        if (Directory.Exists(todayPath) == false)
                        {
                            Directory.CreateDirectory(todayPath);
                        }
                        File.WriteAllText($@"{todayPath}\{ticker}.txt", string.Join("\n", result));
                    }
                }
            }
        }
    }

    public static IList<string> DetectDivergenceByTopAndBottomMfis(string ticker, IList<Price> prices)
    {
        var result = new List<string>();
        var timeBetweenPivots = 60;
        var numberOf5MinInLast1TradingDays = 79;
        var limitHighestMfi = 30;
        var mfis = prices.GetMfi(14).ToList();
        var macds = prices.GetMacd(12, 26, 9).ToList();
        var mfisLast2Days = mfis.Skip(mfis.Count - numberOf5MinInLast1TradingDays).Take(numberOf5MinInLast1TradingDays).ToList();

        var highestMfisOrderByDate = mfisLast2Days.OrderByDescending(m => m.Mfi).Take(limitHighestMfi).OrderBy(m => m.Date).ToList();
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
                    goodToCheck = Math.Abs(timeDiff) >= timeBetweenPivots;
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

        var lowestMfisOrderByDate = mfisLast2Days.OrderBy(m => m.Mfi).Take(limitHighestMfi).OrderBy(m => m.Date).ToList();
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
                    goodToCheck = Math.Abs(timeDiff) >= timeBetweenPivots;
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