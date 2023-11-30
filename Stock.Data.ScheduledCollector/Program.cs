using Stock.Shared;
using Stock.Shared.Models;

namespace Stock.Data.ScheduledCollector
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dbHandler = new DbHandler();
            var tickers = TickersToTrade.POPULAR_TICKERS.Concat(TickersToTrade.CHEAP_TICKERS).ToList();
            foreach (var ticker in tickers)
            {
                dbHandler.FillDbWithTickerPrice(ticker, Timeframe.Daily, DateTime.Now.AddYears(-10)).Wait();
                dbHandler.FillDbWithTickerPrice(ticker, Timeframe.Hour1, DateTime.Now.AddYears(-10)).Wait();
                dbHandler.FillDbWithTickerPrice(ticker, Timeframe.Minute15, DateTime.Now.AddYears(-7)).Wait();
            }
        }
    }
}
