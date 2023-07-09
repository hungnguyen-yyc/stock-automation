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
            var kama = _priceOrderByDateAsc
                .GetKama(10, 2, 30)
                .ToList();
            var efficiencyRatios = kama
                .Select(s => s.ER ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var keltnerChannel = _priceOrderByDateAsc
                .GetKeltner(_parameters.KeltnerEmaPeriod, _parameters.KeltnerMultiplier, _parameters.Atr, CandlePart.HLC3)
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

            var priceTouchingOrAboveKeltnerUpperBand = true;
            var priceTouchingKeltnerUpperBand = true;
            var pricesInGreen = true;

            var priceTouchingOrBelowKeltnerLowerBand = true;
            var priceTouchingKeltnerLowerBand = true;
            var pricesInRed = true;

            var mfiLastNCrossedAbove50 = false;
            var mfiLastNCrossedBelow50 = false;
            var mfiLastNCrossedAbove80 = false;
            var mfiLastNCrossedBelow20 = false;
            var mfiLastNOver50 = false;
            var mfiLastNUnder50 = false;
            var mfiLastNOver80 = false;
            var mfiLastNUnder20 = false;
            var priceTouchingKama = false;

            var priceInsideTheChannel = false;
            var adxUnder25 = false;
            var efficientRatiosUnder25 = efficiencyRatios.Any(x => x < 0.25);

            var last3PriceAndKeltner = _priceOrderByDateAsc
                .Zip(keltnerChannel, (p, k) => new { Price = p, Keltner = k })
                .Skip(_priceOrderByDateAsc.Count - 3)
                .ToList();
            priceTouchingOrAboveKeltnerUpperBand = last3PriceAndKeltner.All(p => 
                ((double)p.Price.High >= (double)p.Keltner.UpperBand && (double)p.Price.Low <= (double)p.Keltner.UpperBand)
                || (double)p.Price.Low > (double)p.Keltner.UpperBand);
            priceTouchingKeltnerUpperBand = last3PriceAndKeltner.All(p =>
                (double)p.Price.High >= (double)p.Keltner.UpperBand && (double)p.Price.Low <= (double)p.Keltner.UpperBand);
            pricesInGreen = last3PriceAndKeltner.All(p => (double)p.Price.Close > (double)p.Price.Open);

            priceTouchingOrBelowKeltnerLowerBand = last3PriceAndKeltner.All(p => 
                (double)p.Price.High >= (double)p.Keltner.LowerBand && (double)p.Price.Low <= (double)p.Keltner.LowerBand
                || (double)p.Price.High < (double)p.Keltner.LowerBand);
            priceTouchingKeltnerLowerBand = last3PriceAndKeltner.All(p =>
                (double)p.Price.High >= (double)p.Keltner.LowerBand && (double)p.Price.Low <= (double)p.Keltner.LowerBand);
            pricesInRed = last3PriceAndKeltner.All(p => (double)p.Price.Close < (double)p.Price.Open);

            var last7PriceAndKeltner = _priceOrderByDateAsc
                .Zip(keltnerChannel, (p, k) => new { Price = p, Keltner = k })
                .Skip(_priceOrderByDateAsc.Count - 7)
                .Take(4)
                .ToList();
            priceInsideTheChannel = last7PriceAndKeltner.All(p => (double)p.Price.High <= (double)p.Keltner.UpperBand && (double)p.Price.Low >= (double)p.Keltner.LowerBand);

            var lastNPriceAndKarma = _priceOrderByDateAsc
                .Zip(kama, (p, k) => new { Price = p, Kama = k })
                .Skip(_priceOrderByDateAsc.Count - 3)
                .ToList();
            priceTouchingKama = lastNPriceAndKarma.Any(p => (double)p.Price.High >= p.Kama.Kama && (double)p.Price.Low <= p.Kama.Kama);

            adxUnder25 = (adx[adx.Count() - 1].Adx ?? 0) <= 25;

            mfiLastNCrossedAbove50 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 50.0).ToList()) == CrossDirection.CROSS_ABOVE;
            mfiLastNCrossedBelow50 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 50.0).ToList()) == CrossDirection.CROSS_BELOW;
            mfiLastNCrossedAbove80 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 80.0).ToList()) == CrossDirection.CROSS_ABOVE;
            mfiLastNCrossedBelow20 = CrossDirectionDetector.GetCrossDirection(mfi, mfi.Select(s => 20.0).ToList()) == CrossDirection.CROSS_BELOW;
            mfiLastNOver50 = mfi.All(m => m > 50);
            mfiLastNUnder50 = mfi.All(m => m < 50);
            mfiLastNOver80 = mfi.Any(m => m > 80);
            mfiLastNUnder20 = mfi.Any(m => m < 20);

            if (!priceTouchingOrAboveKeltnerUpperBand && !priceTouchingOrBelowKeltnerLowerBand)
            {
                return 0.0;
            }

            if (priceTouchingKeltnerUpperBand)
            {
                var rating = 100;
                if (priceTouchingKama)
                {
                    rating -= 10;
                    _description.Append($" | Price touching KAMA: {string.Join(" - ", lastNPriceAndKarma.Select(s => s.Kama.Kama ?? 0).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (efficientRatiosUnder25)
                {
                    rating -= 10;
                    _description.Append($" | Efficiency ratios are under 25: {string.Join(" - ", efficiencyRatios.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (!pricesInGreen)
                {
                    rating -= 10;
                    _description.Append($" | Prices are not in green: {string.Join(" - ", last3PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (!priceInsideTheChannel)
                {
                    rating -= 10;
                    _description.Append($" | Price is not inside the channel: {string.Join(" - ", last7PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (adxUnder25)
                {
                    rating -= 10;
                    _description.Append($" | ADX is under 25: {string.Join(" - ", adx.Select(s => s.Adx).TakeLast(3))}");
                }
                if (mfiLastNCrossedBelow50 || mfiLastNCrossedBelow50)
                {
                    rating -= 10;
                    _description.Append($" | MFI below 50: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (mfiLastNCrossedAbove80 || mfiLastNOver80)
                {
                    rating -= 5;
                    _description.Append($" | MFI above 80: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                return rating;
            }

            if (priceTouchingKeltnerLowerBand)
            {
                var rating = 100;
                if (efficientRatiosUnder25)
                {
                    rating -= 10;
                    _description.Append($" | Efficiency ratios are under 25: {string.Join(" - ", efficiencyRatios.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (priceTouchingKama)
                {
                    rating -= 10;
                    _description.Append($" | Price touching KAMA: {string.Join(" - ", lastNPriceAndKarma.Select(s => s.Kama.Kama ?? 0).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (!pricesInGreen)
                {
                    rating -= 10;
                    _description.Append($" | Prices are not in green: {string.Join(" - ", last3PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (!priceInsideTheChannel)
                {
                    rating -= 10;
                    _description.Append($" | Price is not inside the channel: {string.Join(" - ", last7PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (efficientRatiosUnder25)
                {
                    rating -= 10;
                    _description.Append($" | Efficiency ratios are under 25: {string.Join(" - ", efficiencyRatios.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (adxUnder25)
                {
                    rating -= 10;
                    _description.Append($" | ADX is under 25: {string.Join(" - ", adx.TakeLast(3).Select(s => s.Adx ?? 0).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (mfiLastNCrossedAbove50 || mfiLastNOver50)
                {
                    rating -= 10;
                    _description.Append($" | MFI above 50: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (mfiLastNCrossedBelow20 || mfiLastNUnder20)
                {
                    rating -= 5;
                    _description.Append($" | MFI below 20: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                return rating;
            }

            if (priceTouchingOrAboveKeltnerUpperBand) 
            {
                var rating = 100;
                if (priceTouchingKama)
                {
                    rating -= 10;
                    _description.Append($" | Price touching KAMA: {string.Join(" - ", lastNPriceAndKarma.Select(s => s.Kama.Kama ?? 0).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (efficientRatiosUnder25)
                {
                    rating -= 10;
                    _description.Append($" | Efficiency ratios are under 25: {string.Join(" - ", efficiencyRatios.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (!pricesInRed) 
                {                     
                    rating -= 10;
                    _description.Append($" | Prices are not in red: {string.Join(" - ", last3PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (!priceInsideTheChannel)
                {
                    rating -= 10;
                    _description.Append($" | Price is not inside the channel: {string.Join(" - ", last7PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (adxUnder25)
                {
                    rating -= 10;
                    _description.Append($" | ADX is under 25: {string.Join(" - ", adx.Select(s => s.Adx).TakeLast(3))}");
                }
                if (mfiLastNCrossedBelow50 || mfiLastNCrossedBelow50)
                {
                    rating -= 10;
                    _description.Append($" | MFI below 50: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (mfiLastNCrossedAbove80 || mfiLastNOver80)
                {
                    rating -= 5;
                    _description.Append($" | MFI above 80: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                return rating;
            }
            
            if (priceTouchingOrBelowKeltnerLowerBand)
            {
                var rating = 100;
                if (priceTouchingKama)
                {
                    rating -= 10;
                    _description.Append($" | Price touching KAMA: {string.Join(" - ", lastNPriceAndKarma.Select(s => s.Kama.Kama ?? 0).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (efficientRatiosUnder25)
                {
                    rating -= 10;
                    _description.Append($" | Efficiency ratios are under 25: {string.Join(" - ", efficiencyRatios.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (!pricesInRed)
                {
                    rating -= 10;
                    _description.Append($" | Prices are not in red: {string.Join(" - ", last3PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (!priceInsideTheChannel)
                {
                    rating -= 10;
                    _description.Append($" | Price is not inside the channel: {string.Join(" - ", last7PriceAndKeltner.Select(s => s.Price.Close.Round(2).ToString("0.00")))}");
                }
                if (adxUnder25)
                {
                    rating -= 10;
                    _description.Append($" | ADX is under 25: {string.Join(" - ", adx.TakeLast(3).Select(s => s.Adx ?? 0).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (mfiLastNCrossedAbove50 || mfiLastNOver50)
                {
                    rating -= 10;
                    _description.Append($" | MFI above 50: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                if (mfiLastNCrossedBelow20 || mfiLastNUnder20)
                {
                    rating -= 5;
                    _description.Append($" | MFI below 20: {string.Join(" - ", mfi.TakeLast(3).Select(s => s.Round(2).ToString("0.00")))}");
                }
                return rating;
            }

            return 0.0;
        }
    }
}
