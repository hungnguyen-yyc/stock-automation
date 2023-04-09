using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System.Text;

namespace StockSignalScanner.Strategies
{
    internal class AroonLeadStrategy
    {
        private readonly StringBuilder _description;
        private readonly List<IPrice> _priceOrderByDateAsc;
        private readonly IndicatorParameterPackage _parameters;
        private readonly int _signalInLastNDays;

        public AroonLeadStrategy(IReadOnlyList<IPrice> prices, IndicatorParameterPackage parameters, int signalInLastNDays = 5)
        {
            _priceOrderByDateAsc = prices
                .OrderBy(p => p.Date)
                .Select(p => (IPrice)new HistoricalPrice
                {
                    Date = p.Date,
                    Close = p.Close,
                    High = p.High,
                    Low = p.Low,
                    Open = p.Open,
                    Volume = p.Volume,
                })
                .ToList();

            _parameters = parameters;
            _signalInLastNDays = signalInLastNDays;
            _description = new StringBuilder();
        }

        public double GetRating()
        {
            var aroonCrossDirection = GetAroonOscillatorCrossingDirection();
            if (aroonCrossDirection != CrossDirection.NO_CROSS)
            {
                var rsiRating = GetRsiRating(aroonCrossDirection);
                var mfiRating = GetMfiRating(aroonCrossDirection);
                var stcRating = GetStcRating(aroonCrossDirection);

                var vwmaRating = GetVwmaRating(aroonCrossDirection);
                AddSimpleMovingAverage200Info();

                var volumeRating = GetVolumeRating(aroonCrossDirection);

                AddCandleStickPatternInfo();

                var rating = rsiRating + mfiRating + stcRating + vwmaRating + volumeRating;

                return rating;
            }

            return 0.0;
        }

        public string GetDetails()
        {
            return _description.ToString();
        }

        private void AddSimpleMovingAverage200Info()
        {
            var prices = _priceOrderByDateAsc
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToArray();
            var result = _priceOrderByDateAsc
                .GetSma(200)
                .Select(s => s.Sma ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToArray();
            _description.AppendLine($"\t - SMA:");
            _description.AppendLine($"\t\t - 200   : {string.Join(" - ", result.Select(s => s.Round(2)))}");

            for (int i = 0; i < result.Length; i++)
            {
                if ((double)prices[i].Low <= result[i] && (double)prices[i].High >= result[i])
                {
                    _description.AppendLine($"\t\t - Price touched SMA in last 5 days");
                    break;
                }
            }
        }

        private void AddCandleStickPatternInfo()
        {
            var latest = _priceOrderByDateAsc[_priceOrderByDateAsc.Count() - 1];
            var secondLatest = _priceOrderByDateAsc[_priceOrderByDateAsc.Count() - 2];
            var candlePatterns = CandlestickPatternDetector.Detect(_priceOrderByDateAsc.Skip(_priceOrderByDateAsc.Count() - 3).ToList());

            _description.AppendLine($"\t - Candlestick patterns:");
            if (latest.Close < secondLatest.Close)
            {
                _description.AppendLine($"\t\t - Last 2 closes : Bearish pattern");
            }
            if (latest.Close > secondLatest.Close)
            {
                _description.AppendLine($"\t\t - Last 2 closes : Bullish pattern");
            }
            if (candlePatterns.Any())
            {
                _description.AppendLine($"\t\t - Possible patterns : {string.Join(" - ", candlePatterns)}");
            }
        }

        private double GetVolumeRating(CrossDirection aroonDirection)
        {
            var rating = 20.00;
            var candles = _priceOrderByDateAsc
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays);
            var volumeMA = _priceOrderByDateAsc.Use(CandlePart.Volume)
                .GetSma(_parameters.MovingAverage)
                .Select(s => s.Sma ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToArray();
            var volumeDaily = candles
                .Select(p => (double)p.Volume);
            var pvo = _priceOrderByDateAsc
                .GetPvo(_parameters.PvoFast, _parameters.PvoSlow, _parameters.PvoSignal)
                .Select(s => s.Pvo ?? 0.00)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays);


            var result = volumeDaily.Select((x, i) => x - volumeMA[i]);
            var volumeTrend = TrendChecker.CheckTrend(volumeDaily.ToList());
            var priceTrend = TrendChecker.CheckTrend(candles.Select(s => (double)s.Close).ToList());

            _description.AppendLine($"\t - Volume:");
            _description.AppendLine($"\t\t - Daily : {string.Join((" - "), volumeDaily.Select(s => s.Round(2).ToString("0.00")))}");
            _description.AppendLine($"\t\t - M.A   : {string.Join((" - "), volumeMA.Select(s => s.Round(2)))}");
            _description.AppendLine($"\t\t - Sub   : {string.Join((" - "), result.Select(s => s.Round(2).ToString("0.00;(#0.00)")))}");
            _description.AppendLine($"\t\t - Pvo   : {string.Join((" - "), pvo.Select(s => s.Round(2).ToString("0.00;(#0.00)")))}");
            _description.AppendLine($"\t\t - Trend : {volumeTrend}");

            if (pvo.Last() <= 0)
            {
                rating -= 5;
            }

            /***
             * https://www.rediff.com/money/special/trading-volume-what-it-reveals-about-the-market/20090703.htm
             */
            if (aroonDirection == CrossDirection.CROSS_ABOVE)
            {
                if (priceTrend == TrendChecker.Trend.Decreasing || priceTrend == TrendChecker.Trend.FluctuatingThenDecreasing) 
                {
                    rating -= 5;
                    // bullish
                    if (volumeTrend == TrendChecker.Trend.Decreasing || volumeTrend == TrendChecker.Trend.FluctuatingThenDecreasing) 
                    {
                        rating += 5;
                    }
                    // bearish
                    if (volumeTrend == TrendChecker.Trend.Increasing || volumeTrend == TrendChecker.Trend.FluctuatingThenIncreasing)
                    {
                        rating -= 5;
                    }
                }
                if (priceTrend == TrendChecker.Trend.Increasing || priceTrend == TrendChecker.Trend.Increasing)
                {
                    if (volumeTrend == TrendChecker.Trend.Decreasing || volumeTrend == TrendChecker.Trend.FluctuatingThenDecreasing)
                    {
                        rating -= 5;
                    }
                }
            }

            if (aroonDirection == CrossDirection.CROSS_BELOW)
            {
                if (priceTrend == TrendChecker.Trend.Increasing || priceTrend == TrendChecker.Trend.FluctuatingThenIncreasing)
                {
                    rating -= 5;
                    // bearish
                    if (volumeTrend == TrendChecker.Trend.Decreasing || volumeTrend == TrendChecker.Trend.FluctuatingThenDecreasing)
                    {
                        rating += 5;
                    }
                    // bullish
                    if (volumeTrend == TrendChecker.Trend.Increasing || volumeTrend == TrendChecker.Trend.FluctuatingThenIncreasing)
                    {
                        rating -= 5;
                    }
                }
                // bullish
                if (priceTrend == TrendChecker.Trend.Decreasing || priceTrend == TrendChecker.Trend.Decreasing)
                {
                    if (volumeTrend == TrendChecker.Trend.Decreasing || volumeTrend == TrendChecker.Trend.FluctuatingThenDecreasing)
                    {
                        rating -= 5;
                    }
                }
            }

            _description.AppendLine($"\t\t - Rating: {rating}");

            return rating;
        }

        private CrossDirection GetAroonOscillatorCrossingDirection()
        {
            var result = _priceOrderByDateAsc.GetAroon(_parameters.AroonOscillator)
                .Select(i => i.Oscillator ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var level = result
                .Select(r => 0.00)
                .ToList();
            var crossDirection = CrossDirectionDetector.GetCrossDirection(result, level);

            if (crossDirection == CrossDirection.CROSS_ABOVE)
            {
                _description.Append("\t - Aroon: cross above");
            }
            if (crossDirection == CrossDirection.CROSS_BELOW)
            {
                _description.Append("\t - Aroon: cross below");
            }
            if (result.Any(r => r >= 80))
            {
                _description.Append($" | above 80 in last {_signalInLastNDays} days");
            }
            if (result.Any(r => r <= -80))
            {
                _description.Append($" | below -80 in last {_signalInLastNDays} days");
            }
            _description.AppendLine();

            return crossDirection;
        }

        private double GetVwmaRating(CrossDirection aroonDirection)
        {
            var highVwma = _priceOrderByDateAsc.GetVwma(_parameters.MovingAverage, CandlePart.High)
               .Select(i => i.Vwma ?? 0)
               .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
               .ToList();
            var closeVwma = _priceOrderByDateAsc.GetVwma(_parameters.MovingAverage, CandlePart.Close)
               .Select(i => i.Vwma ?? 0)
               .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
               .ToList();
            var lowVwma = _priceOrderByDateAsc.GetVwma(_parameters.MovingAverage, CandlePart.Low)
               .Select(i => i.Vwma ?? 0)
               .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
               .ToList();

            var latestIndex = highVwma.Count() - 1;
            var latest = _priceOrderByDateAsc[_priceOrderByDateAsc.Count() - 1];
            var secondLatest = _priceOrderByDateAsc[_priceOrderByDateAsc.Count() - 2];
            var rating = 0;

            if (aroonDirection == CrossDirection.CROSS_ABOVE)
            {
                rating = 20;
                if (latest.Close < secondLatest.Close)
                {
                    rating -= 5;
                }
                if (latest.Open > latest.Close)
                {
                    rating -= 5;
                }
                if (latest.Low < (decimal)closeVwma[latestIndex])
                {
                    rating -= 5;
                }
                if (latest.Low < (decimal)lowVwma[latestIndex])
                {
                    rating -= 5;
                }
                if (latest.High < (decimal)closeVwma[latestIndex] || latest.High < (decimal)lowVwma[latestIndex])
                {
                    rating -= rating;
                }
            }

            if (aroonDirection == CrossDirection.CROSS_BELOW)
            {
                rating = 20;

                if (latest.Close > secondLatest.Close)
                {
                    rating -= 5;
                }
                if (latest.Open < latest.Close)
                {
                    rating -= 5;
                }
                if (latest.High > (decimal)closeVwma[latestIndex])
                {
                    rating -= 5;
                }
                if (latest.High > (decimal)highVwma[latestIndex])
                {
                    rating -= 5;
                }
                if (latest.Low > (decimal)closeVwma[latestIndex] || latest.Low > (decimal)highVwma[latestIndex])
                {
                    rating -= rating;
                }
            }

            _description.AppendLine($"\t - VWMA:");
            _description.AppendLine($"\t\t - Price : {latest.Low} - {latest.Close} - {latest.High}");
            _description.AppendLine($"\t\t - VWMA  : {lowVwma[latestIndex].Round(2)} - {closeVwma[latestIndex].Round(2)} - {highVwma[latestIndex].Round(2)}");
            _description.AppendLine($"\t\t - Rating: {rating}");

            return rating;
        }

        private double GetStcRating(CrossDirection aroonDirection)
        {
            var result = SchaffTrendCycle
                .CalculateSTC(_priceOrderByDateAsc, _parameters.StcShort, _parameters.StcLong, _parameters.StcCycleLength, (decimal)_parameters.StcFactor)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var rating = 0;
            if (aroonDirection == CrossDirection.CROSS_ABOVE)
            {
                var level = result
                .Select(r => 25m)
                .ToList();
                var crossDirection = CrossDirectionDetector.GetCrossDirection(result, level);
                /***
                 * best if it can cross 25 level
                 * if not, check if it's in the bullish trend (>25, >75) for STC
                 * similar with bearish
                 */
                if (crossDirection == CrossDirection.CROSS_ABOVE)
                {
                    rating = 20;
                }
                else
                {
                    if (result.All(i => i >= 75))
                    {
                        rating = 20;
                    }
                    else if (result.All(i => i > 25))
                    {
                        rating = 15;
                    }
                }
            }
            else if (aroonDirection == CrossDirection.CROSS_BELOW)
            {
                var level = result
                .Select(r => 75m)
                .ToList();
                var crossDirection = CrossDirectionDetector.GetCrossDirection(result, level);
                if (crossDirection == CrossDirection.CROSS_BELOW)
                {
                    rating = 20;
                }
                else
                {
                    if (result.All(i => i <= 25))
                    {
                        rating = 20;
                    }
                    else if (result.All(i => i <= 75))
                    {
                        rating = 15;
                    }
                }
            }

            _description.AppendLine($"\t - STC:");
            _description.AppendLine($"\t\t - STC   : {string.Join(" - ", result.Select(s => s.Round(2)))}");
            _description.AppendLine($"\t\t - Rating: {rating}");

            return rating;
        }

        private double GetMfiRating(CrossDirection aroonDirection)
        {
            var result = _priceOrderByDateAsc.GetMfi(_parameters.Mfi)
                .Select(i => i.Mfi ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var level = result
                .Select(r => 50.00)
                .ToList();
            var crossDirection = CrossDirectionDetector.GetCrossDirection(result, level);
            var rating = 20.00;

            if (aroonDirection == CrossDirection.CROSS_ABOVE)
            {
                if (result[result.Count - 1] <= 50)
                {
                    rating -= rating;
                }
                else
                {
                    if (crossDirection == CrossDirection.CROSS_ABOVE)
                    {
                        rating = 20;
                    }
                    else if (crossDirection == CrossDirection.NO_CROSS)
                    {
                        var trend = TrendChecker.CheckTrend(result);
                        if (trend == TrendChecker.Trend.Decreasing || trend == TrendChecker.Trend.FluctuatingThenDecreasing)
                        {
                            rating -= 10;
                        }
                    }
                }

                for (int i = result.Count - 1; i > 0; i--)
                {
                    if (result[i] >= 80)
                    {
                        rating -= 10;
                        break;
                    }
                }
            }
            else if (aroonDirection == CrossDirection.CROSS_BELOW)
            {
                if (result[result.Count - 1] >= 50)
                {
                    rating -= rating;
                }
                else
                {
                    if (crossDirection == CrossDirection.CROSS_BELOW)
                    {
                        rating = 20;
                    }
                    else if (crossDirection == CrossDirection.NO_CROSS)
                    {
                        var trend = TrendChecker.CheckTrend(result);
                        // This is because we're already bearish
                        if (trend == TrendChecker.Trend.Increasing || trend == TrendChecker.Trend.FluctuatingThenIncreasing)
                        {
                            rating -= 5;
                        }

                    }
                }


                for (int i = result.Count - 1; i > 0; i--)
                {
                    if (result[i] <= 20)
                    {
                        rating -= 5;
                        break;
                    }
                }
            }

            _description.AppendLine($"\t - MFI:");
            _description.AppendLine($"\t\t - MFI   : {string.Join(" - ", result.Select(s => s.Round(2)))}");
            _description.AppendLine($"\t\t - Rating: {rating}");


            if (result.Any(s => s >= 80))
            {
                _description.AppendLine($"\t\t - Warning: OVERBOUGHT");
            }
            if (result.Any(s => s <= 20))
            {
                _description.AppendLine($"\t\t - Warning: OVERSOLD");
            }

            return rating;
        }

        private double GetRsiRating(CrossDirection aroonDirection)
        {
            var result = _priceOrderByDateAsc.GetRsi(_parameters.Rsi)
                .Select(i => i.Rsi ?? 0)
                .Skip(_priceOrderByDateAsc.Count() - _signalInLastNDays)
                .ToList();
            var level = result
                .Select(r => 50.00)
                .ToList();
            var crossDirection = CrossDirectionDetector.GetCrossDirection(result, level);
            var rating = 0.0;
            var ratingReasons = new StringBuilder();

            if (aroonDirection == CrossDirection.CROSS_ABOVE)
            {
                if (result[result.Count - 1] <= 50)
                {
                    rating = 0.0;
                } 
                else
                {
                    if (crossDirection == CrossDirection.CROSS_ABOVE)
                    {
                        rating = 20;
                    } 
                    else if (crossDirection == CrossDirection.NO_CROSS)
                    {
                        var trend = TrendChecker.CheckTrend(result);
                        if (trend == TrendChecker.Trend.Decreasing || trend == TrendChecker.Trend.FluctuatingThenDecreasing)
                        {
                            rating -= 10;
                        }
                    }
                }

                for (int i = result.Count - 1; i > 0; i--)
                {
                    if (result[i] >= 70)
                    {
                        rating -= 5;
                        break;
                    }
                }
            }
            else if (aroonDirection == CrossDirection.CROSS_BELOW)
            {
                if (result[result.Count - 1] >= 50)
                {
                    rating = 0.0;
                }
                else
                {
                    if (crossDirection == CrossDirection.CROSS_BELOW)
                    {
                        rating = 20;
                    }
                    else if (crossDirection == CrossDirection.NO_CROSS)
                    {
                        var trend = TrendChecker.CheckTrend(result);
                        // This is because we're already bearish
                        if (trend == TrendChecker.Trend.Increasing || trend == TrendChecker.Trend.FluctuatingThenIncreasing)
                        {
                            rating -= 10;
                        }
                    }
                }

                for (int i = result.Count - 1; i > 0; i--)
                {
                    if (result[i] <= 30)
                    {
                        rating -= 5;
                        break;
                    }
                }
            }

            _description.AppendLine($"\t - RSI:");
            _description.AppendLine($"\t\t - RSI   : {string.Join(" - ", result.Select(s => s.Round(2)))}");
            _description.AppendLine($"\t\t - Rating: {rating}");

            if (result.Any(s => s >= 70))
            {
                _description.AppendLine($"\t\t - Warning: OVERBOUGHT");
            }
            if (result.Any(s => s <= 30))
            {
                _description.AppendLine($"\t\t - Warning: OVERSOLD");
            }

            return rating;
        }
    }
}
