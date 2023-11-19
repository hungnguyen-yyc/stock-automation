using Newtonsoft.Json;
using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day_trading_signals
{
    internal class KamaMfiEmasRunner
    {
        public static async Task Run(List<string> favs, int interval)
        {
            using (var httpClient = new HttpClient())
            {
                foreach (var ticker in favs)
                {
                    interval = 15;
                    var ago5years = DateTime.Now.AddYears(-5).ToString("yyyy-MM-dd");
                    string API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/{interval}min/{ticker}?apikey={MyProcessHelpers.API_KEY}";
                    string API_ENDPOINT_1HOUR = $"https://financialmodelingprep.com/api/v3/historical-chart/1hour/{ticker}?apikey={MyProcessHelpers.API_KEY}";
                    string API_ENDPOINT_4HOUR = $"https://financialmodelingprep.com/api/v3/historical-chart/4hour/{ticker}?&apikey={MyProcessHelpers.API_KEY}";
                    string API_ENDPOINT_DAILY = $"https://financialmodelingprep.com/api/v3/historical-price-full/{ticker}?from={ago5years}&apikey={MyProcessHelpers.API_KEY}";

                    var response = await httpClient.GetAsync(API_ENDPOINT);
                    var responseHour = await httpClient.GetAsync(API_ENDPOINT_1HOUR);
                    var response4Hour = await httpClient.GetAsync(API_ENDPOINT_4HOUR);
                    var responseDaily = await httpClient.GetAsync(API_ENDPOINT_DAILY);

                    if (response.IsSuccessStatusCode && responseHour.IsSuccessStatusCode)
                    {
                        string contentMinute = await response.Content.ReadAsStringAsync();
                        var tickerMinutePrices = JsonConvert.DeserializeObject<IList<Price>>(contentMinute, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        var content1Hour = await responseHour.Content.ReadAsStringAsync();
                        var tickerHistoricalPricesHour = JsonConvert.DeserializeObject<IList<Price>>(content1Hour, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        var content4Hour = await response4Hour.Content.ReadAsStringAsync();
                        var tickerHistoricalPrices4Hour = JsonConvert.DeserializeObject<IList<Price>>(content4Hour, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        var contentDaily = await responseDaily.Content.ReadAsStringAsync();
                        var tickerHistoricalPricesDaily = JsonConvert.DeserializeObject<TickerHistoricalPrice>(contentDaily, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (tickerMinutePrices == null || tickerHistoricalPrices4Hour == null || tickerHistoricalPricesHour == null || tickerHistoricalPricesDaily == null)
                        {
                            return;
                        }

                        var pricesDaily = tickerHistoricalPricesDaily.Historical.Select(p => (Price)p).Reverse().ToList();
                        var prices4Hour = tickerHistoricalPrices4Hour.Reverse().ToList();
                        var prices1Hour = tickerHistoricalPricesHour.Reverse().ToList();
                        var prices = tickerMinutePrices.Reverse().ToList();
                        var resultBySwingHighsLows = DetectKaufmanMfi(ticker, prices, prices1Hour, prices4Hour, pricesDaily);

                        if (resultBySwingHighsLows.Any())
                        {
                            resultBySwingHighsLows.Reverse();
                            var todayPath = $@"{MyProcessHelpers.PATH}\{DateTime.Now.ToString("yyyy-MM-dd")}";
                            if (Directory.Exists(MyProcessHelpers.PATH) == false)
                            {
                                Directory.CreateDirectory(MyProcessHelpers.PATH);
                            }
                            if (Directory.Exists(todayPath) == false)
                            {
                                Directory.CreateDirectory(todayPath);
                            }
                            File.AppendAllText($@"{todayPath}\{ticker}-kaufman-mfi-{interval}-mins.txt", "\n" + string.Join("\n", resultBySwingHighsLows));
                        }
                    }
                }
            }
        }

        public static List<string> DetectKaufmanMfi(string ticker, IList<Price> minutePrices, IList<Price> hour1Price, IList<Price> hour4Price, IList<Price> dailyPrice)
        {
            var emaDaily = dailyPrice.GetEma(25);
            var emaHour4 = hour4Price.GetEma(25);
            var emaHour1 = hour1Price.GetEma(25);
            var kamaMinute = minutePrices.GetKama(25, 5, 30);
            var kamaDaily = dailyPrice.GetKama(25, 5, 30);
            var mfi = minutePrices.GetMfi(14);
            var result = new List<string>();

            for (int i = 5; i < kamaMinute.Count(); i++)
            {
                var startIndex = i - 5;
                var last5Kama = kamaMinute.Skip(startIndex).Take(5).Select(p => (decimal)(p.Kama ?? 0)).ToList();
                var lastMfi = mfi.ToList()[i].Mfi;
                var last5Price = minutePrices.Skip(startIndex).Take(5).Select(p => p.Close).ToList();
                var priceCrossedKama = CrossDirectionDetector.GetCrossDirection(last5Price, last5Kama);
                if (priceCrossedKama == CrossDirection.CROSS_ABOVE && lastMfi >= 60)
                {
                    result.Add(ticker + " " + minutePrices.ToList()[i].Date.ToString("yyyy-MM-dd HH:mm:ss") + " " + lastMfi + " " + priceCrossedKama);
                }

                if (priceCrossedKama == CrossDirection.CROSS_BELOW && lastMfi <= 40)
                {
                    result.Add(ticker + " " + minutePrices.ToList()[i].Date.ToString("yyyy-MM-dd HH:mm:ss") + " " + lastMfi + " " + priceCrossedKama);
                }
            }

            return result;
        }
    }
}
