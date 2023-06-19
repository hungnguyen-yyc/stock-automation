using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class EmasCrossLeadsStrategy : AbstractIndicatorPackageStrategy
    {
        public EmasCrossLeadsStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5) : base(prices, parameters, signalInLastNDays)
        {
        }

        public override double GetRating()
        {
            var emaCrossDirection = GetEmasCrossingDirection();
            if (emaCrossDirection != CrossDirection.NO_CROSS)
            {
                var rsiRating = GetRsiRating(emaCrossDirection);
                var mfiRating = GetMfiRating(emaCrossDirection);
                var macdRating = GetMacdCrossingDirection(emaCrossDirection);
                var aroonRating = GetAroonOscillatorCrossingDirection(emaCrossDirection);

                var vwmaRating = GetVwmaRating(emaCrossDirection);
                AddSimpleMovingAverage200Info();

                var volumeRating = GetVolumeRating(emaCrossDirection);

                AddCandleStickPatternInfo();

                var rating = rsiRating + mfiRating + vwmaRating + volumeRating + macdRating + aroonRating;

                return rating;
            }

            return 0.0;
        }

        public CrossDirection GetEmasCrossingDirection()
        {
            var emaShort = _priceOrderByDateAsc.GetEma(_parameters.EmaShort)
                .Select(i => i.Ema ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var emaLong = _priceOrderByDateAsc.GetEma(_parameters.EmaLong)
                .Select(i => i.Ema ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var crossDirection = CrossDirectionDetector.GetCrossDirection(emaShort, emaLong);

            _description.AppendLine($"\t - EMAS: " + crossDirection);
            return crossDirection;
        }

        /// <summary>
        /// Get Macd Rating. Maximum is 16
        /// </summary>
        /// <param name="emaCrossDirection"></param>
        /// <returns></returns>
        public double GetMacdCrossingDirection(CrossDirection emaCrossDirection)
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

            _description.AppendLine($"\t - Macd: {macd[_signalInLastNDays - 1].ToString("0.00")} - Signal: {signal[_signalInLastNDays - 1].ToString("0.00")}");

            if (emaCrossDirection == CrossDirection.CROSS_ABOVE)
            {
                if (macd.Last() > signal.Last())
                {

                    if (macd.Any(r => r < 0))
                    {
                        rating = 16;
                        _description.AppendLine("\t\t - Cross Signal Under 0");
                    }
                    else
                    {
                        rating = 0;
                        _description.AppendLine("\t\t - Cross Signal Above 0");
                    }
                }
                else
                {
                    rating = 0.0;
                    _description.AppendLine("\t\t - Macd < Signal");
                }
            }

            if (emaCrossDirection == CrossDirection.CROSS_BELOW)
            {
                if (macd.Last() < signal.Last())
                {

                    if (macd.Any(r => r > 0))
                    {
                        rating = 16;
                        _description.AppendLine("\t\t - Cross Signal Under 0");
                    }
                    else
                    {
                        rating = 0;
                        _description.AppendLine("\t\t - Cross Signal Above 0");
                    }
                }
                else
                {
                    rating = 0.0;
                    _description.AppendLine("\t\t - Macd > Signal");
                }
            }

            _description.AppendLine($"\t\t - Rating: {rating}");

            return rating;
        }

        /// <summary>
        /// Get Aroon Rating. Maximum is 16
        /// </summary>
        /// <param name="emaCrossDirection"></param>
        /// <returns></returns>
        private double GetAroonOscillatorCrossingDirection(CrossDirection emaCrossDirection)
        {
            var result = _priceOrderByDateAsc.GetAroon(_parameters.AroonOscillator)
                .Select(i => i.Oscillator ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var levelPlus50 = result
                .Select(r => 50.00)
                .ToList();
            var levelMinus50 = result
                .Select(r => 50.00)
                .ToList();
            var level0 = result
                .Select(r => 0.00)
                .ToList();
            var rating = 15.00;
            var crossDirection = CrossDirectionDetector.GetCrossDirection(result, level0);
            var crossDirectionPlus50 = CrossDirectionDetector.GetCrossDirection(result, levelPlus50);
            var crossDirectionMinus50 = CrossDirectionDetector.GetCrossDirection(result, levelMinus50);

            _description.AppendLine("\t - Aroon:");

            if (emaCrossDirection == CrossDirection.CROSS_ABOVE)
            {
                if (result.Last() >= 50)
                {
                    _description.AppendLine("\t\t - Above or Equal 50");
                    if (result.Last() > result[result.Count - 2])
                    {
                        rating = 16;
                        _description.AppendLine("\t\t - Increase last 2 days");
                    }
                    else
                    {
                        rating = 10;
                    }
                }
                else
                {
                    rating = 5;
                    _description.AppendLine("\t\t - Above or Equal Above 0");
                }
            }

            if (emaCrossDirection == CrossDirection.CROSS_BELOW)
            {
                if (result.Last() <= -50)
                {
                    _description.AppendLine("\t\t - Below or Equal -50");
                    if (result.Last() < result[result.Count - 2])
                    {
                        rating = 16;
                        _description.AppendLine("\t\t - Decrease last 2 days");
                    }
                    else
                    {
                        rating = 10;
                    }
                }
                else
                {
                    rating = 5;
                    _description.AppendLine("\t\t - Below or Equal Below 0");
                }
            }


            if (result.Any(r => r >= 80))
            {
                _description.AppendLine($"\t\t - Above 80 in last {_signalInLastNDays} days");
            }
            if (result.Any(r => r <= -80))
            {
                _description.AppendLine($"\t\t - Below -80 in last {_signalInLastNDays} days");
            }
            _description.AppendLine($"\t\t - Rating: {rating}");

            return rating;
        }
    }
}
