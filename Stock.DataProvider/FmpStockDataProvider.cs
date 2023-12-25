using Newtonsoft.Json;
using Stock.Shared.Helpers;
using Stock.Shared.Models;
using System.Diagnostics;

namespace Stock.DataProvider
{
    // Provider to get data from https://site.financialmodelingprep.com/
    public class FmpStockDataProvider
    {
        private const string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";

        public async Task<IList<Price>?> CollectData(string ticker, Timeframe timeframe, DateTime from)
        {
            return await CollectData(ticker, timeframe, from, DateTime.Now);
        }

        public async Task<IList<Price>?> CollectData(string ticker, Timeframe timeframe, DateTime from, DateTime to)
        {
            using var httpClient = new HttpClient();
            var prices = new List<Price>();
            var fromDate = from.ToString("yyyy-MM-dd");
            var toDate = to.ToString("yyyy-MM-dd");

            while (from < to)
            {
                try
                {
                    var interval = FmpTimeframeHelper.GetTimeframe(timeframe);
                    var API_ENDPOINT = string.Empty;

                    if (timeframe == Timeframe.Daily)
                    {
                        // with dayly timeframe, we need to specify from date, or it will fetch all data from the beginning
                        API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/{interval}/{ticker}?from={fromDate}&to={toDate}&apikey={API_KEY}";
                    }
                    else
                    {
                        API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/{interval}/{ticker}?from={fromDate}&to={toDate}&apikey={API_KEY}";
                    }

                    var response = await httpClient.GetAsync(API_ENDPOINT);

                    if (response.IsSuccessStatusCode)
                    {
                        string contentMinute = await response.Content.ReadAsStringAsync();
                        var priceByDateRange = JsonConvert.DeserializeObject<IList<Price>>(contentMinute, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (priceByDateRange == null || !priceByDateRange.Any())
                        {
                            return prices;
                        }

                        prices.AddRange(priceByDateRange);
                    }

                    if (prices.Last().Date.Date == to.Date || !prices.Any())
                    {
                        break;
                    }

                    to = prices.Min(p => p.Date);
                    toDate = to.ToString("yyyy-MM-dd");
                    fromDate = to.AddMonths(-2).ToString("yyyy-MM-dd");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error fetching data for {ticker} from {fromDate} to {to}", ex);
                }
            }

            return prices.Distinct().ToList();
        }
    }
}
