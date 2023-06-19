using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class AroonLeadStrategy : AbstractIndicatorPackageStrategy
    {
        public AroonLeadStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5) : base(prices, parameters, signalInLastNDays)
        {
        }
    }
}
