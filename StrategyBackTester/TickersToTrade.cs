﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrategyBackTester
{
    internal class TickersToTrade
    {
#if !DEBUG
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "AVGO", "SCHW" };
        public static readonly List<string> CHEAP_TICKERS = new List<string> { "TMF", "PTON", "QS", "IOVA", "CLSK", "UNG", "JDST", "RIG", "RXRX", "SWN", "VFS", "AMC", "RDFN", "SOFI", "NIO", "CLVT", "YMM", "NU", "HOOD", "TME", "SOXS", "ROIV", "NYCB", "VTRS", "AMCR", "SPXU", "PSQ", "UVXY", "LYFT", "F", "WBD", "HBAN", "TAL", "RUN", "MARA", "TSLY", "KEY", "RIOT", "QID", "ELAN", "FHN", "IONQ", "AAL", "SNAP", "PR", "SPXS", "LTHM", "ET", "SH", "TSLL", "GT", "SPDN", "PARA", "TOST", "NCLH", "CCL", "STNE", "ARRY", "M", "JWN", "HPE", "SQQQ", "RF", "CPNG", "T", "VFC", "RIVN", "CLF", "AEO", "KMI", "XPEV", "PCG", "CHWY", "BITO", "PATH", "PLTR", "GPS" };
#else
        public static readonly List<string> POPULAR_TICKERS = new List<string> { "AMD"};
        public static readonly List<string> CHEAP_TICKERS = new List<string> { "TMF", "PTON", "QS", "IOVA", "CLSK", "UNG", "JDST", "RIG", "RXRX", "SWN", "VFS", "AMC", "RDFN", "SOFI", "NIO", "CLVT", "YMM", "NU", "HOOD", "TME", "SOXS", "ROIV", "NYCB", "VTRS", "AMCR", "SPXU", "PSQ", "UVXY", "LYFT", "F", "WBD", "HBAN", "TAL", "RUN", "MARA", "TSLY", "KEY", "RIOT", "QID", "ELAN", "FHN", "IONQ", "AAL", "SNAP", "PR", "SPXS", "LTHM", "ET", "SH", "TSLL", "GT", "SPDN", "PARA", "TOST", "NCLH", "CCL", "STNE", "ARRY", "M", "JWN", "HPE", "SQQQ", "RF", "CPNG", "T", "VFC", "RIVN", "CLF", "AEO", "KMI", "XPEV", "PCG", "CHWY", "BITO", "PATH", "PLTR", "GPS" };
#endif
    }
}