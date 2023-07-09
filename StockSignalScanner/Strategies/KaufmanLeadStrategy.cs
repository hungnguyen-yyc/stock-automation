using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class KaufmanLeadStrategy : AbstractIndicatorPackageStrategy
    {
        public KaufmanLeadStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5) : base(prices, parameters, signalInLastNDays)
        {
        }

        public override double GetRating()
        {
            var pvos = _priceOrderByDateAsc
                .GetPvo(_parameters.PvoFast, _parameters.PvoSlow, _parameters.PvoSignal)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .Select(s => s.Pvo ?? 0)
                .ToList();

            var mfis = _priceOrderByDateAsc
                .GetMfi(_parameters.Mfi)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .Select(s => s.Mfi ?? 0)
                .ToList();

            var kama50 = _priceOrderByDateAsc
                .Use(CandlePart.OHLC4)
                .GetKama(50)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .Select(s => s.Kama ?? 0.0)
                .ToList();

            var kama250 = _priceOrderByDateAsc
                .Use(CandlePart.OHLC4)
                .GetKama(250)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .Select(s => s.Kama ?? 0.0)
                .ToList();

            var er = _priceOrderByDateAsc
                .Use(CandlePart.Close)
                .GetKama(10)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .Select(s => s.ER);

            var pvosCrossAbove0LevelDirection = CrossDirectionDetector.GetCrossDirection(pvos, pvos.Select(s => 0.0).ToList()) > CrossDirection.CROSS_ABOVE;
            var pvoLast3Above0 = pvos.Last() > 0 && pvos.Skip(pvos.Count - 3).All(s => s > 0);
            var pvoIncreasedLast3 = pvos.Last() > pvos[pvos.Count - 2] && pvos[pvos.Count - 2] > pvos[pvos.Count - 3];

            var mfisCrossLevelDirection = CrossDirectionDetector.GetCrossDirection(mfis, mfis.Select(s => 50.0).ToList());
            var mfisAbove50Level = mfis.All(s => s >= 50);
            var mfisBelow50Level = mfis.All(s => s <= 50);

            var erAbove50Level = er.All(s => s >= 50);
            var erAbove25LessThan50Level = er.All(s => s >= 25 && s < 50);

            var kama50Flags = new List<bool>
            {
                kama50[1] > kama50[0]
            };
            for (int i = 1; i < kama50.Count; i++)
            {
                if (kama50[i] > kama50[i - 1])
                {
                    kama50Flags.Add(true);
                }
                else
                {
                    kama50Flags.Add(false);
                }
            }

            var kama250Flags = new List<bool>
            {
                kama250[1] > kama250[0]
            };
            for (int i = 1; i < kama250.Count; i++)
            {
                if (kama250[i] > kama250[i - 1])
                {
                    kama250Flags.Add(true);
                }
                else
                {
                    kama250Flags.Add(false);
                }
            }

            var kama50And250HasSameLast3FlagValues = kama50Flags
                .Skip(kama50Flags.Count - 2)
                .Zip(
                    kama250Flags
                    .Skip(kama250Flags.Count - 2), (kama50Flag, kama250Flag) => kama50Flag == kama250Flag)
                .All(s => s);

            var kama50LastNAboveKama250LastN = kama50
                .Zip(
                    kama250, (kama50Value, kama250Value) => kama50Value > kama250Value)
                .All(s => s);

            var kama250LastNAboveKama50LastN = kama50
                .Zip(
                    kama250, (kama50Value, kama250Value) => kama50Value < kama250Value)
                .All(s => s);

            // kama50LastNAboveKama250LastN and kama250LastNAboveKama50LastN are true
            // means kama50 and kama250 are in same direction and interwined with each other
            // means no clear trend
            var kamaNoClearTrend = kama50LastNAboveKama250LastN && kama250LastNAboveKama50LastN;

            var kama50AndKama250BothInsidePrice = (double)_priceOrderByDateAsc.Last().High >= kama50.Last() && (double)_priceOrderByDateAsc.Last().High >= kama250.Last() 
                && (double)_priceOrderByDateAsc.Last().Low <= kama50.Last() && (double)_priceOrderByDateAsc.Last().Low <= kama250.Last();

            if (kama50AndKama250BothInsidePrice)
            {
                return 0;
            }

            var priceCrossKama50Direction = CrossDirectionDetector.GetCrossDirection(_priceOrderByDateAsc.Use(CandlePart.OHLC4).Select(s => s.Value).ToList(), kama50);
            var priceCrossKama250Direction = CrossDirectionDetector.GetCrossDirection(_priceOrderByDateAsc.Use(CandlePart.OHLC4).Select(s => s.Value).ToList(), kama250);

            var kamaSameFlagRating = kama50And250HasSameLast3FlagValues ? 10 : 0;

            if (kama50LastNAboveKama250LastN && priceCrossKama50Direction == CrossDirection.CROSS_ABOVE)
            {
                _description.Append($" | Price Crossed Above Kama 50 Above 250 | ");
            }

            return base.GetRating();
        }
    }
}
