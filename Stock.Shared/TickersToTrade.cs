namespace Stock.Shared
{
    public class TickersToTrade
    {
#if !DEBUG
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "SCHW", "MARA", "RIOT", "CVNA", "PLTR", "NIO", "AAL", "TD", "XOM", "CHWY", "SBUX", "ENB.TO", "SU.TO", "BNS.TO", "TD.TO", "RY.TO", "SHOP.TO", "BMO.TO", "BITF.TO", "CNQ.TO", "AC.TO", "T.TO", "MFC.TO", "SLF.TO", "%5EBTCUSD", "%5EDOGEUSD", "%5EDOTUSD", "%5EADAUSD" };
#else
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "SCHW", "MARA", "RIOT", "CVNA", "PLTR", "NIO", "AAL", "TD", "XOM", "CHWY", "SBUX", "ENB.TO", "SU.TO", "BNS.TO", "TD.TO", "RY.TO", "SHOP.TO", "BMO.TO", "BITF.TO", "CNQ.TO", "AC.TO", "T.TO", "MFC.TO", "SLF.TO", "%5EBTCUSD", "%5EDOGEUSD", "%5EDOTUSD", "%5EADAUSD" };
#endif
    }
}
