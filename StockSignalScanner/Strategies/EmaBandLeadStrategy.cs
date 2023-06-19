using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class EmaBandLeadStrategy : AbstractIndicatorPackageStrategy
    {
        public EmaBandLeadStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5) : base(prices, parameters, signalInLastNDays)
        {
        }

        public override double GetRating()
        {
            var rating = 0.0;
            var ema5 = _priceOrderByDateAsc.Use(CandlePart.HLC3).GetEma(5).Select(e => e.Ema ?? 0).Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays).ToList();
            var ema75 = _priceOrderByDateAsc.Use(CandlePart.HLC3).GetEma(75).Select(e => e.Ema ?? 0).Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays).ToList();
            var ema200 = _priceOrderByDateAsc.Use(CandlePart.HLC3).GetEma(200).Select(e => e.Ema ?? 0).Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays).ToList();
            var price = _priceOrderByDateAsc.Select(p => (double)p.Close).Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays).ToList();
            var currentPrice = _priceOrderByDateAsc.Last();

            var priceCross75Above = CrossDirectionDetector.GetCrossDirection(price, ema75) == CrossDirection.CROSS_ABOVE;
            var priceCross200Below = CrossDirectionDetector.GetCrossDirection(price, ema200) == CrossDirection.CROSS_BELOW;
            var ema5Cross75Above = CrossDirectionDetector.GetCrossDirection(ema5, ema75) == CrossDirection.CROSS_ABOVE;
            var ema5Cross200Below = CrossDirectionDetector.GetCrossDirection(ema5, ema200) == CrossDirection.CROSS_BELOW;

            var macd = _priceOrderByDateAsc.GetMacd(_parameters.MacdFast, _parameters.MacdSlow, _parameters.MacdSignal)
                .Select(i => i.Macd ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var signal = _priceOrderByDateAsc.GetMacd(_parameters.MacdFast, _parameters.MacdSlow, _parameters.MacdSignal)
                .Select(i => i.Signal ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();

            if (ema5Cross200Below || priceCross200Below)
            {
                _description.AppendLine($"\t - EMA 5 OR PRICE CROSS BELOW 200");
                if (ema200.Last() < ema75.Last())
                {
                    _description.AppendLine($"\t\t - EMA 200 < EMA 75");
                    rating = 100.0;
                }
                else
                {
                    _description.AppendLine($"\t\t - EMA 200 < EMA 75");
                    rating = 90.0;
                }
                if (macd.Last() < signal.Last())
                {
                    _description.AppendLine($"\t\t - MACD < SIGNAL");
                    if (macd.Last() < 0)
                    {
                        _description.AppendLine($"\t\t - MACD < 0");
                        rating -= 0.0;
                    }
                    else
                    {
                        _description.AppendLine($"\t\t - MACD > 0");
                        rating -= 8.0;
                    }
                }
                else
                {
                    _description.AppendLine($"\t\t - MACD > SIGNAL");
                    rating -= 16.0;
                }
                var mfiRating = GetMfiRating(CrossDirection.CROSS_BELOW);
                rating = rating - (17 - mfiRating);
            }

            if (ema5Cross75Above || priceCross75Above)
            {
                _description.AppendLine($"\t - EMA 5 OR PRICE CROSS ABOVE 75");
                if (ema200.Last() > ema75.Last())
                {
                    rating = 90;
                    _description.AppendLine($"\t\t - EMA 200 > EMA 75");
                } 
                else
                {
                    rating = 100.0;
                    _description.AppendLine($"\t\t - EMA 200 < EMA 75");
                }
                if (macd.Last() > signal.Last())
                {
                    _description.AppendLine($"\t\t - MACD > SIGNAL");
                    if (macd.Last() < 0)
                    {
                        _description.AppendLine($"\t\t - MACD > 0");
                        rating -= 0.0;
                    }
                    else
                    {
                        _description.AppendLine($"\t\t - MACD < 0");
                        rating -= 8.0;
                    }
                }
                else
                {
                    _description.AppendLine($"\t\t - MACD < SIGNAL");
                    rating -= 16.0;
                }
                var mfiRating = GetMfiRating(CrossDirection.CROSS_ABOVE);
                rating = rating - (17 - mfiRating);
            }

            if (ema5.Last() < (double)Math.Min(currentPrice.Open, currentPrice.Close) 
                || ema5.Last() > (double)Math.Max(currentPrice.Open, currentPrice.Close)) 
            {
                rating -= 5;
                _description.AppendLine($"\t\t - EMA 5 Not in today price range.");
                return rating;
            }

            return rating;
        }
    }
}
