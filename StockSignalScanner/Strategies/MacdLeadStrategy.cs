using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class MacdLeadStrategy : AbstractIndicatorPackageStrategy
    {
        public MacdLeadStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5) : base(prices, parameters, signalInLastNDays)
        {
        }

        public override double GetRating()
        {
            (double macdRating, CrossDirection aroonCrossDirection) = GetMacdCrossingDirection();
            if (aroonCrossDirection != CrossDirection.NO_CROSS)
            {
                var rsiRating = GetRsiRating(aroonCrossDirection);
                var mfiRating = GetMfiRating(aroonCrossDirection);
                var stcRating = GetStcRating(aroonCrossDirection);

                var vwmaRating = GetVwmaRating(aroonCrossDirection);
                AddSimpleMovingAverage200Info();

                var volumeRating = GetVolumeRating(aroonCrossDirection);

                AddCandleStickPatternInfo();

                var rating = rsiRating + mfiRating + stcRating + vwmaRating + volumeRating + macdRating;

                return rating;
            }

            return 0.0;
        }

        public (double, CrossDirection) GetMacdCrossingDirection()
        {
            var macd = _priceOrderByDateAsc.GetMacd(_parameters.MacdFast, _parameters.MacdSlow, _parameters.MacdSignal)
                .Select(i => i.Macd ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var signal = _priceOrderByDateAsc.GetMacd(_parameters.MacdFast, _parameters.MacdSlow, _parameters.MacdSignal)
                .Select(i => i.Signal ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var crossDirection = CrossDirectionDetector.GetCrossDirection(macd, signal);
            var rating = 0.0;

            _description.AppendLine($"\t - Macd: {macd[_signalInLastNDays - 1]} - Signal: {signal[_signalInLastNDays - 1]}");

            if (crossDirection == CrossDirection.CROSS_ABOVE)
            {
                if (macd.Any(r => r < 0))
                {
                    rating = 15;
                    _description.AppendLine("\t\t - Cross Signal Under 0");
                }
                else
                {
                    rating = 5;
                    _description.AppendLine("\t\t - Cross Signal Above 0");
                }
            }
            if (crossDirection == CrossDirection.CROSS_BELOW)
            {
                if (macd.Any(r => r > 0))
                {
                    rating = 15;
                    _description.AppendLine("\t\t - Cross Signal Above 0");
                }
                else
                {
                    rating = 5;
                    _description.AppendLine("\t\t - Cross Signal Under 0");
                }
            }

            _description.AppendLine($"\t\t - Rating: {rating}");

            return (rating, crossDirection);
        }
    }
}
