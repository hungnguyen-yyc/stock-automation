using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Models
{
    public class CryptoData : StockDataAggregator
    {
        public CryptoData(string ticker, string exchange, IList<HistoricalPrice> prices, int rsiPeriod, int rsiMA, int macdShortPeriod, int macdLongPeriod, int macdSignalPeriod, int stochasticPeriod, int smoothK, int smoothD) : base(ticker, exchange, prices, rsiPeriod, rsiMA, macdShortPeriod, macdLongPeriod, macdSignalPeriod, stochasticPeriod, smoothK, smoothD)
        {
        }
    }
}
