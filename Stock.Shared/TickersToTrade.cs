namespace Stock.Shared
{
    public class TickersToTrade
    {
#if !DEBUG
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "MARA", "RIOT", "CVNA", "PLTR", "TD", "GME", "CHWY", "SMCI" };
#else
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "SPY", "QQQ", "MARA", "RIOT", "TD", "XOM", "GME", "ENB.TO", "SU.TO", "BNS.TO", "TD.TO", "RY.TO", "SHOP.TO", "BMO.TO", "CNQ.TO", "MFC.TO", "SLF.TO" };
#endif
    }
}
