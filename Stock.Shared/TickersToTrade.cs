namespace Stock.Shared
{
    public class TickersToTrade
    {
#if !DEBUG
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "SCHW", "MARA", "RIOT", "CVNA", "PLTR", "NIO", "AAL", "TD" };
#else
        //TODO: fix this barchart.com for crypto add %5E(^) before the ticker
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "%5EBTCUSD", "%5EDOGEUSD", "%5EGRTUSD", "%5EDOTUSD", "%5ESUIUSDT" };
#endif
    }
}
