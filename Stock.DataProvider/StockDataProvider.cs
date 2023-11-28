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

            while (from < to)
            {
                var nowDate = to.ToString("yyyy-MM-dd");
                var interval = FmpTimeframeHelper.GetTimeframe(timeframe);
                var API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/{interval}/{ticker}?to={nowDate}&apikey={API_KEY}";
                Debug.WriteLine(API_ENDPOINT);

                var response = await httpClient.GetAsync(API_ENDPOINT);

                if (response.IsSuccessStatusCode)
                {
                    string contentMinute = await response.Content.ReadAsStringAsync();
                    var priceByDateRange = JsonConvert.DeserializeObject<IList<Price>>(contentMinute, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (priceByDateRange == null)
                    {
                        return null;
                    }

                    prices.AddRange(priceByDateRange);
                }

                if (prices.Last().Date == to)
                {
                    break;
                }

                to = prices.Last().Date;
            }

            return prices.Distinct().ToList();
        }
    }
}
