using Newtonsoft.Json;
using Stock.Shared.Helpers;
using Stock.Shared.Models;

namespace Stock.Strategies.Helpers
{
    public class StockDataCollector
    {
        private const string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";

        public async Task<IList<Price>?> CollectData(string ticker, Timeframe timeframe, DateTime from)
        {
            using var httpClient = new HttpClient();
            var fromDate = from.ToString("yyyy-MM-dd");
            var nowDate = DateTime.Now.ToString("yyyy-MM-dd");
            var interval = FmpTimeframeHelper.GetTimeframe(timeframe);
            var API_ENDPOINT = $"https://financialmodelingprep.com/api/v3/historical-chart/{interval}/{ticker}?from={fromDate}&to={nowDate}&apikey={API_KEY}";

            var response = await httpClient.GetAsync(API_ENDPOINT);

            if (response.IsSuccessStatusCode)
            {
                string contentMinute = await response.Content.ReadAsStringAsync();
                var prices = JsonConvert.DeserializeObject<IList<Price>>(contentMinute, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                if (prices == null)
                {
                    return null;
                }

                return prices;
            }

            return null;
        }
    }
}
