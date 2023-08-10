using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockSignalScanner.Models;

namespace day_trading_signals
{
    internal static class RunEveryPeriod
    {

        public static async Task Run(List<string> favs, int interval)
        {
            using (var httpClient = new HttpClient())
            {
                foreach (var ticker in favs)
                {
                    string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/technical_indicator/{interval}min/{ticker}?period=50&type=tema&apikey={MyProcessHelpers.API_KEY}";

                    var response = await httpClient.GetAsync(API_ENDPOINT);

                    if (response.IsSuccessStatusCode)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        var tickerHistoricalPrices = JsonConvert.DeserializeObject<IList<Price>>(content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (tickerHistoricalPrices == null)
                        {
                            return;
                        }
                        //var resultByTopAndBottomMfis = DetectDivergenceByTopAndBottomMfis(ticker, tickerHistoricalPrices.Reverse().ToList());

                        //if (resultByTopAndBottomMfis.Any())
                        //{
                        //    resultByTopAndBottomMfis = resultByTopAndBottomMfis.Reverse().ToList();
                        //    var todayPath = $@"{MyProcessHelpers.PATH}\{DateTime.Now.ToString("yyyy-MM-dd")}";
                        //    if (Directory.Exists(MyProcessHelpers.PATH) == false)
                        //    {
                        //        Directory.CreateDirectory(MyProcessHelpers.PATH);
                        //    }
                        //    if (Directory.Exists(todayPath) == false)
                        //    {
                        //        Directory.CreateDirectory(todayPath);
                        //    }
                        //    File.WriteAllText($@"{todayPath}\{ticker}-top-bottom-mfis-5-mins.txt", string.Join("\n", resultByTopAndBottomMfis));
                        //}


                        var resultBySwingHighsLows = DetectDivergenceBySwingHighsOrLows(ticker, tickerHistoricalPrices.Reverse().ToList());
                        if (resultBySwingHighsLows.Any())
                        {
                            resultBySwingHighsLows = resultBySwingHighsLows.Reverse().ToList();
                            resultBySwingHighsLows = resultBySwingHighsLows.Take(5).ToList();
                            var todayPath = $@"{MyProcessHelpers.PATH}\{DateTime.Now.ToString("yyyy-MM-dd")}";
                            if (Directory.Exists(MyProcessHelpers.PATH) == false)
                            {
                                Directory.CreateDirectory(MyProcessHelpers.PATH);
                            }
                            if (Directory.Exists(todayPath) == false)
                            {
                                Directory.CreateDirectory(todayPath);
                            }
                            File.WriteAllText($@"{todayPath}\{ticker}-swing-highs-lows-{interval}-mins.txt", string.Join("\n", resultBySwingHighsLows));
                        }
                    }
                }
            }
        }

        private static IList<string> DetectDivergenceBySwingHighsOrLows(string ticker, IList<Price> prices)
        {
            var result = new List<string>();
            var swingHighs = MyProcessHelpers.FindSwingHighs(prices.ToList<Price>(), MyProcessHelpers.NUMBER_OF_SWING_POINTS);
            var swingLows = MyProcessHelpers.FindSwingLows(prices.ToList<Price>(), MyProcessHelpers.NUMBER_OF_SWING_POINTS);
            var trend = MyProcessHelpers.DetermineTrend(swingHighs, swingLows);
            var mfis = prices.GetMfi(9).ToList();
            var macds = prices.GetMacd(12, 26, 9).ToList();

            //var swingHighsLast1Days = swingHighs.Where(m => m.Date.Date.CompareTo(DateTime.Today) == 0).ToList();
            //var swingLowsLast1Days = swingLows.Where(m => m.Date.Date.CompareTo(DateTime.Today) == 0).ToList();

            var swingHighsLast1Days = swingHighs;
            var swingLowsLast1Days = swingLows;

            var startDateMap = new Dictionary<DateTime, IList<string>>();
            var hourCheck = (int priceHour) =>
            {
                if (priceHour == 15 || priceHour == 3 || priceHour == 16 || priceHour == 4)
                {
                    return false;
                }
                return true;
            };

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

                    var goodToCheck = true;
                    if (price1Day == price2Day)
                    {
                        var timeDiff = price1Hour * 60 + price1Minute - (price2Hour * 60 + price2Minute);
                        goodToCheck = Math.Abs(timeDiff) >= MyProcessHelpers.TIME_BETWEEN_PIVOTS;
                    }

                    goodToCheck = hourCheck(price2Hour);

                    if (price2.Date.Date.CompareTo(DateTime.Today) != 0)
                    {
                        goodToCheck = false;
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
                            temaString = $"*** Close ({price2.Close}) <= Tema ({price2.Tema.ToString("0.##")}) ***";
                        }

                        if (mfiResult1 == null || mfiResult2 == null)
                        {
                            continue;
                        }

                        if (mfiResult1.Mfi > mfiResult2.Mfi && price1.High <= price2.High)
                        {
                            resultString = $"- {ticker}: Bearish Divergence: {price1.Date} and {price2.Date}";
                        }
                        else if (mfiResult2.Mfi < mfiResult1.Mfi && price2.High < price1.High)
                        {
                            resultString = $"- {ticker}: Bearish Convergence: {price1.Date} and {price2.Date}";
                        }

                        if (!string.IsNullOrEmpty(resultString))
                        {
                            if (!startDateMap.ContainsKey(price1.Date))
                            {
                                startDateMap.Add(price1.Date, new List<string>() { resultString });
                            }
                            else
                            {
                                startDateMap[price1.Date].Add(resultString);
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
                        goodToCheck = Math.Abs(timeDiff) >= MyProcessHelpers.TIME_BETWEEN_PIVOTS;
                    }

                    goodToCheck = hourCheck(price2Hour);

                    if (price2.Date.Date.CompareTo(DateTime.Today) != 0)
                    {
                        goodToCheck = false;
                    }

                    if (goodToCheck)
                    {
                        var mfiResult1 = mfis.FirstOrDefault(m => m.Date == price1.Date);
                        var mfiResult2 = mfis.FirstOrDefault(m => m.Date == price2.Date);
                        var macd = macds.FirstOrDefault(m => m.Date == price2.Date);

                        var macdString = "";
                        var temaString = "";
                        var resultString = "";
                        if (macd != null && macd.Macd > macd.Signal)
                        {
                            macdString = $"*** MACD ({macd.Macd.Value.ToString("0.##")}) > Signal ({macd.Signal.Value.ToString("0.##")}) ***";
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
                            resultString = $"+ {ticker}: Bullish Divergence: {price1.Date} and {price2.Date}";
                        }
                        else if (mfiResult2.Mfi > mfiResult1.Mfi && price2.Low > price1.Low)
                        {
                            resultString = $"+ {ticker}: Bullish Convergence: {price1.Date} and {price2.Date}";
                        }

                        if (!string.IsNullOrEmpty(resultString))
                        {
                            if (!startDateMap.ContainsKey(price1.Date))
                            {
                                startDateMap.Add(price1.Date, new List<string>() { resultString });
                            }
                            else
                            {
                                startDateMap[price1.Date].Add(resultString);
                            }
                        }
                    }
                }
            }


            foreach (var item in startDateMap)
            {
                result.AddRange(item.Value);
            }
            return result;
        }

        private static IList<string> DetectDivergenceByTopAndBottomMfis(string ticker, IList<Price> prices)
        {
            var result = new List<string>();
            var mfis = prices.GetMfi(9).ToList();
            var macds = prices.GetMacd(12, 26, 9).ToList();
            var mfisLast2Days = mfis.Where(m => m.Date.Date.CompareTo(DateTime.Today) == 0).ToList();

            var highestMfisOrderByDate = mfisLast2Days.OrderByDescending(m => m.Mfi).Take(MyProcessHelpers.TOP_OR_BOTTOM_LIMIT).OrderBy(m => m.Date).ToList();
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
                        goodToCheck = Math.Abs(timeDiff) >= MyProcessHelpers.TIME_BETWEEN_PIVOTS;
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
                            resultString = $"- {ticker}: Bearish Divergence: {price1.Date} and {price2.Date}";
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

            var lowestMfisOrderByDate = mfisLast2Days.OrderBy(m => m.Mfi).Take(MyProcessHelpers.TOP_OR_BOTTOM_LIMIT).OrderBy(m => m.Date).ToList();
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
                        goodToCheck = Math.Abs(timeDiff) >= MyProcessHelpers.TIME_BETWEEN_PIVOTS;
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
                            resultString = $"- {ticker}: Bullish Divergence: {price1.Date} and {price2.Date}";
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
    }
}