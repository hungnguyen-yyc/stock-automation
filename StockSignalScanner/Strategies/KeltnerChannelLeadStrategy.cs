using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class KeltnerChannelLeadStrategy : AbstractIndicatorPackageStrategy
    {
        public KeltnerChannelLeadStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5) : base(prices, parameters, signalInLastNDays)
        {
        }

        public override double GetRating()
        {
            var keltnerChannel = _priceOrderByDateAsc
                .GetKeltner(_parameters.KeltnerEmaPeriod, _parameters.KeltnerMultiplier, _parameters.Atr, CandlePart.HLC3)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays - 5)
                .ToList();
            var keltnerUpperBand = keltnerChannel.Select(i => i.UpperBand ?? 0).ToList();
            var keltnerLowerBand = keltnerChannel.Select(i => i.LowerBand ?? 0).ToList();
            var adx = _priceOrderByDateAsc
                .GetAdx(_parameters.Adx)
                .ToList();
            var mfi = _priceOrderByDateAsc
                .GetMfi(_parameters.Mfi)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .Select(i => i.Mfi ?? 0)
                .ToList();

            var priceTouchingKeltnerUpperBand = false;
            var priceAboveKeltnerUpperBand = false;
            var pricesInGreen = false;

            var priceTouchingKeltnerLowerBand = false;
            var priceBelowKeltnerLowerBand = false;
            var pricesInRed = false;

            var mfiLastNCrossedAbove50 = false;
            var mfiLastNCrossedBelow50 = false;
            var mfiLastNCrossedAbove80 = false;
            var mfiLastNCrossedBelow20 = false;
            var mfiLastNOver50 = false;
            var mfiLastNUnder50 = false;
            var mfiLastNOver80 = false;
            var mfiLastNUnder20 = false;

            var priceInsideTheChannel = false;
            var adxUnder25 = false;
            for (int i = _priceOrderByDateAsc.Count - 1; i >= _priceOrderByDateAsc.Count - 7; i--)
            {
                var price = _priceOrderByDateAsc[i];
                var keltnerUpperBandValue = keltnerUpperBand[i];
                var keltnerLowerBandValue = keltnerLowerBand[i];
                if (i <= _priceOrderByDateAsc.Count - 1 && i >= _priceOrderByDateAsc.Count - 3)
                {
                    priceTouchingKeltnerUpperBand = (double)price.High >= keltnerUpperBandValue && (double)price.Low <= keltnerUpperBandValue;
                    priceAboveKeltnerUpperBand = (double)price.Low > keltnerUpperBandValue;
                    pricesInGreen = (double)price.Close > (double)price.Open;

                    priceTouchingKeltnerLowerBand = (double)price.High >= keltnerLowerBandValue && (double)price.Low <= keltnerLowerBandValue;
                    priceBelowKeltnerLowerBand = (double)price.High < keltnerLowerBandValue;
                    pricesInRed = (double)price.Close < (double)price.Open;
                } 
                else
                {
                    priceInsideTheChannel = (double)price.High <= keltnerUpperBandValue && (double)price.Low >= keltnerLowerBandValue;
                }
            }
            
            adxUnder25 = (adx[adx.Count() - 1].Adx ?? 0) <= 25;

            mfiLastNCrossedAbove50 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 50.0).ToList()) == CrossDirection.CROSS_ABOVE;
            mfiLastNCrossedBelow50 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 50.0).ToList()) == CrossDirection.CROSS_BELOW;
            mfiLastNCrossedAbove80 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 80.0).ToList()) == CrossDirection.CROSS_ABOVE;
            mfiLastNCrossedBelow20 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 20.0).ToList()) == CrossDirection.CROSS_BELOW;
            mfiLastNOver50 = mfi.All(m => m > 50);
            mfiLastNUnder50 = mfi.All(m => m < 50);
            mfiLastNOver80 = mfi.Any(m => m > 80);
            mfiLastNUnder20 = mfi.Any(m => m < 20);

            if (!priceTouchingKeltnerUpperBand && !priceAboveKeltnerUpperBand && !priceTouchingKeltnerLowerBand && !priceBelowKeltnerLowerBand)
            {
                return 0.0;
            }

            if (priceTouchingKeltnerUpperBand || priceAboveKeltnerUpperBand) 
            {
                var rating = 100;
                if (!pricesInGreen)
                {
                    _description.Append($"Prices (last 3) are not in green {_priceOrderByDateAsc.TakeLast(3).Select(s => s.Close)}");
                    rating -= 10;
                }
                if (adxUnder25)
                {
                    _description.Append($" | ADX is under 25: {adx.TakeLast(3)}");
                    rating -= 10;
                }
                if (mfiLastNCrossedBelow50 || mfiLastNCrossedBelow50)
                {
                    _description.Append($" | MFI below 50: {mfi.TakeLast(3)}");
                    rating -= 10;
                }
                if (mfiLastNCrossedAbove80 || mfiLastNOver80)
                {
                    _description.Append($" | MFI above 80: {mfi.TakeLast(3)}");
                    rating -= 5;
                }
                return rating;
            }
            
            if (priceTouchingKeltnerLowerBand || priceBelowKeltnerLowerBand)
            {
                var rating = 100;
                if(!pricesInRed)
                {
                    _description.Append($"Prices (last 3) are not in red {_priceOrderByDateAsc.TakeLast(3).Select(s => s.Close)}");
                    rating -= 10;
                }
                if (adxUnder25)
                {
                    _description.Append($" | ADX is under 25: {adx.TakeLast(3)}");
                    rating -= 10;
                }
                if (mfiLastNCrossedAbove50 || mfiLastNOver50)
                {
                    _description.Append($" | MFI above 50: {mfi.TakeLast(3)}");
                    rating -= 10;
                }
                if (mfiLastNCrossedBelow20 || mfiLastNUnder20)
                {
                    _description.Append($" | MFI below 20: {mfi.TakeLast(3)}");
                    rating -= 5;
                }
                return rating;
            }

            return 0.0;
        }
    }
}
