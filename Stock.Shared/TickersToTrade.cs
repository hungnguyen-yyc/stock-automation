namespace Stock.Shared
{
    public class TickersToTrade
    {
#if !DEBUG
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "SCHW", "MARA", "RIOT", "CVNA", "PLTR", "NIO", "AAL", "TD", "XOM", "CHWY", "SBUX" };
#else
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "SCHW", "MARA", "RIOT", "CVNA", "PLTR", "NIO", "AAL", "TD", "XOM", "CHWY", "SBUX" };
#endif
    }
}
