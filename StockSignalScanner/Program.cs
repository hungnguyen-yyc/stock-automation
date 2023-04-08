using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using StockSignalScanner.Strategies;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace StockSignalScanner
{
    public partial class Program
    {
        static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";

        public static async Task Main(string[] args)
        {
            var runScanStock = args.Any(a => a.ToLower().Contains("runscanstock") || a.ToLower().Contains("run-scan-stock"));
            var runScanStock15m = args.Any(a => a.ToLower().Contains("runscanstock15m") || a.ToLower().Contains("run-scan-stock-15m"));
            // var failed = new List<string>() { "ATEST-A", "BTAL", "HIBS", "IIGD", "TOPS", "USFR", "WEBS" };
            var hot = new List<string>() { "AMD", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "GME", "AMC" };
            using (var httpClient = new HttpClient())
            {
                if (runScanStock15m)
                {
                    await StockScannerInterday.Run(hot.ToArray(), API_KEY);
                    return;
                }
                if (runScanStock)
                {
                    // https://financialmodelingprep.com/api/v3/financial-statement-symbol-lists?apikey=e2b2a6d07ebf89ca33bb96b0b590daab
                    var northAmericaStocks = await StockScanner.GetStocksFromUSCANExchanges(999999999, 3000000, 1000, 1, API_KEY);

                    if (runScanStock)
                    {
                        await StockScanner.StartScan(northAmericaStocks, API_KEY);
                    }
                    return;
                }
            }
        }
    }
}
