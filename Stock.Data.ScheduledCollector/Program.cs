using Stock.Shared;
using Stock.Shared.Models;
using System.Diagnostics;

namespace Stock.Data.ScheduledCollector
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        private static void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        static async Task Run()
        {
            var dbHandler = new StockDataRepository();
            var tickers = TickersToTrade.POPULAR_TICKERS.ToList();
            foreach (var ticker in tickers)
            {
                Log($"Collecting data for {ticker} at {DateTime.Now:s}");
                await dbHandler.QuickFill(ticker, Timeframe.Minute15, DateTime.Today.AddDays(-60));
                await dbHandler.QuickFill(ticker, Timeframe.Minute30, DateTime.Today.AddDays(-60));
                await dbHandler.QuickFill(ticker, Timeframe.Hour1, DateTime.Today.AddDays(-60));
                await dbHandler.QuickFill(ticker, Timeframe.Daily, DateTime.Today.AddDays(-60));
                Log($"Finished collecting data for {ticker} at {DateTime.Now:s}");
                Log("+++++++++++++++++++++++++++++++++++++++++");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            Log("Finished collecting data for all tickers. Waiting for next trigger.");
        }
    }
}
