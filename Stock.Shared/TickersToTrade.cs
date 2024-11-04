namespace Stock.Shared
{
    public class TickersToTrade
    {
#if !DEBUG
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "SCHW", "MARA", "RIOT", "CVNA", "PLTR", "NIO", "AAL", "TD", "XOM", "GME", "CHWY", "SBUX", "ENB.TO", "SU.TO", "BNS.TO", "TD.TO", "RY.TO", "SHOP.TO", "BMO.TO", "BITF.TO", "CNQ.TO", "AC.TO", "T.TO", "MFC.TO", "SLF.TO" };
#else
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "SPY", "QQQ", "MARA", "RIOT", "CVNA", "PLTR", "TD", "XOM", "GME", "CHWY", "ENB.TO", "SU.TO", "BNS.TO", "TD.TO", "RY.TO", "SHOP.TO", "BMO.TO", "BITF.TO", "CNQ.TO", "AC.TO", "T.TO", "MFC.TO", "SLF.TO" };
#endif
    }
}
