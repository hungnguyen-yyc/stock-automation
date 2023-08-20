using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockSignalScanner.Models;
using StockSignalScanner.Indicators;
using log4net.Util;

namespace day_trading_signals
{
    internal static class KaufmanMfiRunner
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

                        var prices = tickerHistoricalPrices.Reverse().ToList();
                        var resultBySwingHighsLows = DetectKaufmanMfi(ticker, prices);

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
                            File.WriteAllText($@"{todayPath}\{ticker}-kaufman-mfi-{interval}-mins.txt", string.Join("\n", resultBySwingHighsLows));
                        }
                    }
                }
            }
        }

        private static IList<string> DetectKaufmanMfi(string ticker, IList<Price> prices)
        {
            var result = new List<string>();

            var kaufman25 = prices.GetKama(25).Select(x => x.Kama ?? 0).ToList();
            var kaufman100 = prices.GetKama(100).Select(x => x.Kama ?? 0).ToList();
            var ema200 = prices.GetEma(200).Select(x => x.Ema ?? 0).ToList();
            var mfis = Indicator.GetMfi<Price>(prices, 14);

            var closeLast5 = prices.Select(e => (double)e.Close).Skip(prices.Count - 5).ToList();
            var kaufman25Last5 = prices.GetKama(25).Select(x => x.Kama ?? 0).Skip(prices.Count - 5).ToList();
            var kaufman100Last5 = prices.GetKama(100).Select(x => x.Kama ?? 0).Skip(prices.Count - 5).ToList();

            var priceCrossKama25Direction = CrossDirectionDetector.GetCrossDirection(closeLast5, kaufman25Last5);
            var priceCrossKama100Direction = CrossDirectionDetector.GetCrossDirection(closeLast5, kaufman100Last5);

            if (priceCrossKama25Direction == CrossDirection.CROSS_ABOVE)
            {
                var isMfiOver60 = (mfis.Last().Mfi ?? 0) >= 60;
                var isKauffman25Last5Over100Last5 = kaufman25Last5.Zip(kaufman100.Skip(prices.Count - 5)).All(x => x.First > x.Second);
                var isPriceOverEma50 = closeLast5.Zip(ema200.Skip(prices.Count - 5)).All(x => x.First > x.Second);
                if (isMfiOver60 && isPriceOverEma50 && isKauffman25Last5Over100Last5)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price cross above Kama 25 - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }

            }
            else if (priceCrossKama25Direction == CrossDirection.CROSS_BELOW)
            {
                var isMfiBelow40 = (mfis.Last().Mfi ?? 0) <= 40;
                var isPriveBelowEma50 = closeLast5.Zip(ema200.Skip(prices.Count - 5)).All(x => x.First < x.Second);
                var isKauffman25Last5Below100Last5 = kaufman25Last5.Zip(kaufman100.Skip(prices.Count - 5)).All(x => x.First < x.Second);
                if (isMfiBelow40 && isPriveBelowEma50 && isKauffman25Last5Below100Last5)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price cross below Kama 25 - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }
            }
            else if (priceCrossKama100Direction == CrossDirection.CROSS_BELOW)
            {
                var isKauffman25Last5Below100Last5 = kaufman25Last5.Zip(kaufman100.Skip(prices.Count - 5)).All(x => x.First < x.Second);
                if (isKauffman25Last5Below100Last5)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price cross below Kama 100 - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }
            }
            else if (priceCrossKama100Direction == CrossDirection.CROSS_ABOVE)
            {
                var isKauffman25Last5Over100Last5 = kaufman25Last5.Zip(kaufman100.Skip(prices.Count - 5)).All(x => x.First > x.Second);
                if (isKauffman25Last5Over100Last5)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price cross above Kama 100 - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }
            }
            else
            {
                // check for touch
                var price = Enumerable.Last<Price>(prices);
                var kaufman25Last = kaufman25.Last();
                var isMfiOver60 = (mfis.Last().Mfi ?? 0) >= 60;
                var isMfiUnder40 = (mfis.Last().Mfi ?? 0) <= 40;
                var kama25InPriceRange = (double)price.Low <= kaufman25Last && (double)price.High >= kaufman25Last;
                var kama25InSecondLastPriceRange = (double)prices[prices.Count - 2].Low <= kaufman25[prices.Count - 2] && (double)prices[prices.Count - 2].High >= kaufman25[prices.Count - 2];
                var last14PriceUnderKama25Last14 = prices.Skip(prices.Count - 6).Take(5).Zip(kaufman25.Skip(prices.Count - 6)).Take(5).All(x => (double)x.First.Close <= x.Second);
                var last14PriceAboveKama25Last14 = prices.Skip(prices.Count - 6).Take(5).Zip(kaufman25.Skip(prices.Count - 6)).Take(5).All(x => (double)x.First.Close >= x.Second);

                if (kama25InPriceRange && last14PriceUnderKama25Last14 && !isMfiOver60)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price touch Kama 25 from under - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }
                else if (kama25InPriceRange && last14PriceAboveKama25Last14 && !isMfiUnder40)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price touch Kama 25 from above - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }
                else if (kama25InPriceRange && kama25InSecondLastPriceRange)
                {
                    result.Add($"{ticker} - {Enumerable.Last<Price>(prices).Date} - Price touch Kama 25 consolidating - Mfi: {(mfis.Last().Mfi ?? 0).ToString("0.##")} - Kama25: {kaufman25.Last().ToString("0.##")} - Kama: 100{kaufman100.Last().ToString("0.##")} - Ema50: {ema200.Last().ToString("0.##")}");
                }
            }

            return result;
        }
    }
}