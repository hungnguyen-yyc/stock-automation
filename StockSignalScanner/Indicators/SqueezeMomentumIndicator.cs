using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    public static class SqueezeMomentumIndicator
    {
        public static (List<decimal> SqueezeIndicator, List<decimal> MomentumIndicator, List<bool> SqueezeStart, List<bool> SqueezeStop) Calculate(List<HistoricalPrice> prices, int bollingerBandPeriod, decimal bollingerBandStandardDeviation, int keltnerChannelPeriod, int atrPeriod)
        {
            // Calculate the Bollinger Bands values
            var bollingerBands = BollingerBands.Calculate(prices, bollingerBandPeriod, bollingerBandStandardDeviation);

            // Calculate the Keltner Channel values
            var keltnerChannels = KeltnerChannel.Calculate(prices, keltnerChannelPeriod, atrPeriod);

            // Initialize lists to store the Squeeze Indicator and Momentum Indicator values
            var squeezeIndicator = new List<decimal>();
            var momentumIndicator = new List<decimal>();
            var squeezeStart = new List<bool>();
            var squeezeStop = new List<bool>();

            // Iterate through the Bollinger Bands and Keltner Channel values
            for (int i = 0; i < bollingerBands.Count; i++)
            {
                // Get the current Bollinger Bands and Keltner Channel values
                (decimal lowerBollingerBand, decimal upperBollingerBand) = bollingerBands[i];
                (decimal lowerKeltnerChannel, decimal upperKeltnerChannel) = keltnerChannels[i];

                // Calculate the Squeeze Indicator value
                decimal squeezeIndicatorValue = (upperBollingerBand - lowerKeltnerChannel) / (upperKeltnerChannel - lowerKeltnerChannel);

                // Calculate the Momentum Indicator value
                decimal momentumIndicatorValue = 100 * ((squeezeIndicatorValue - 0.5m) / 0.5m);

                // Determine if the squeeze has started or stopped
                bool squeezeHasStarted = (i == 0 || squeezeIndicator[i - 1] < 0.5m) && squeezeIndicatorValue >= 0.5m;
                bool squeezeHasStopped = (i == 0 || squeezeIndicator[i - 1] >= 0.5m) && squeezeIndicatorValue < 0.5m;

                // Add the Squeeze Indicator, Momentum Indicator, and squeeze start/stop indicators to the lists
                squeezeIndicator.Add(squeezeIndicatorValue);
                momentumIndicator.Add(momentumIndicatorValue);
                squeezeStart.Add(squeezeHasStarted);
                squeezeStop.Add(squeezeHasStopped);
            }

            // Return the Squeeze Indicator, Momentum Indicator, and squeeze start/stop indicators
            return (squeezeIndicator, momentumIndicator, squeezeStart, squeezeStop);
        }

        public static (bool IsSqueeze, DateTimeOffset SqueezeStart, DateTimeOffset SqueezeEnd) CalculateV2(List<HistoricalPrice> prices, int bollingerPeriod, decimal bollingerStandardDeviation, int keltnerPeriod, int keltnerAtrPeriod)
        {
            // Calculate the Bollinger Band values
            var bollingerBands = BollingerBands.Calculate(prices, bollingerPeriod, bollingerStandardDeviation);

            // Calculate the Keltner Channel values
            var keltnerChannels = KeltnerChannel.Calculate(prices, keltnerPeriod, keltnerAtrPeriod);

            // Initialize variables to track the squeeze and squeeze start/end
            bool isSqueeze = false;
            DateTimeOffset squeezeStart = default;
            DateTimeOffset squeezeEnd = default;

            // Iterate through the Bollinger Band and Keltner Channel values
            for (int i = 0; i < bollingerBands.Count; i++)
            {
                // Get the current Bollinger Band and Keltner Channel values, and the current date
                (decimal LowerBollingerBand, decimal UpperBollingerBand) = bollingerBands[i];
                (decimal LowerKeltnerChannel, decimal UpperKeltnerChannel) = keltnerChannels[i];
                DateTimeOffset date = prices[i].Date;

                // Check if the Bollinger Band values are within the Keltner Channel values
                if (LowerBollingerBand >= LowerKeltnerChannel && UpperBollingerBand <= UpperKeltnerChannel)
                {
                    // The Bollinger Band values are within the Keltner Channel values, so the squeeze is in progress
                    isSqueeze = true;

                    // Check if the squeeze has just started
                    if (squeezeStart == default)
                    {
                        // The squeeze has just started
                        squeezeStart = date;
                    }
                }
                else
                {
                    // The Bollinger Band values are not within the Keltner Channel values, so the squeeze has ended
                    isSqueeze = false;

                    // Check if the squeeze has just ended
                    if (squeezeStart != default && squeezeEnd == default)
                    {
                        // The squeeze has just ended
                        squeezeEnd = date;
                    }
                }
            }

            // Return the squeeze and squeeze start/end dates
            return (isSqueeze, squeezeStart, squeezeEnd);
        }

        public static (bool IsSqueeze, List<DateTimeOffset> SqueezeStarts, List<DateTimeOffset> SqueezeEnds) CalculateV3(List<HistoricalPrice> prices, int bollingerPeriod, double bollingerMultFactor, int keltnerPeriod, double keltnerMultFactor, bool useTrueRange)
        {
            // Initialize variables to track the squeeze and squeeze start/end
            bool isSqueeze = false;
            var squeezeStarts = new List<DateTimeOffset>();
            var squeezeEnds = new List<DateTimeOffset>();

            // Calculate the moving average
            var ma = prices.Sma(p => Convert.ToDouble(p.Close), bollingerPeriod);

            // Calculate the Bollinger Band values
            var basis = ma;
            var dev = prices.Stdev(p => Convert.ToDouble(p.Close), bollingerPeriod).Select(d => bollingerMultFactor * d).ToArray();
            var upperBB = basis.Select((b, i) => b + dev[i]).ToArray();
            var lowerBB = basis.Select((b, i) => b - dev[i]).ToArray();

            // Calculate the Keltner Channel values
            var range = useTrueRange ? prices.Select(p => p.TrueRange()) : prices.Select(p => Convert.ToDouble(p.High - p.Low)).ToArray();
            var rangema = range.Sma(r => r, keltnerPeriod).ToArray();
            var upperKC = ma.Select((m, i) => m + rangema[i] * keltnerMultFactor).ToArray();
            var lowerKC = ma.Select((m, i) => m - rangema[i] * keltnerMultFactor).ToArray();

            // Iterate through the Bollinger Band and Keltner Channel values
            for (int i = 0; i < prices.Count; i++)
            {
                // Get the current Bollinger Band and Keltner Channel values, and the current date
                double lowerBBValue = lowerBB[i];
                double upperBBValue = upperBB[i];
                double lowerKCValue = lowerKC[i];
                double upperKCValue = upperKC[i];
                DateTimeOffset date = prices[i].Date;

                // Check if the Bollinger Band values are within the Keltner Channel values (indicating a squeeze)
                bool sqzOn = lowerBBValue > lowerKCValue && upperBBValue < upperKCValue;
                if (sqzOn)
                {
                    // If the previous value was not in a squeeze, then this is the start of a new squeeze
                    if (!isSqueeze)
                    {
                        squeezeStarts.Add(date);
                    }

                    // Update the squeeze status
                    isSqueeze = true;
                }
                else
                {
                    // If the previous value was in a squeeze, then this is the end of the squeeze
                    if (isSqueeze)
                    {
                        squeezeEnds.Add(date);
                    }

                    // Update the squeeze status
                    isSqueeze = false;
                }
            }

            // Return the squeeze status, start, and end dates
            return (isSqueeze, squeezeStarts, squeezeEnds);
        }

    }

    public static class EnumerableExtensions
    {
        public static IEnumerable<double> Stdev(this IEnumerable<HistoricalPrice> prices, Func<HistoricalPrice, double> selector, int period)
        {
            return Enumerable.Range(0, prices.Count()).Select(i =>
            {
                if (i < period - 1) return double.NaN;
                var values = prices.Skip(i - period + 1).Take(period).Select(selector);
                var avg = values.Average();
                var sumOfSquaresOfDifferences = values.Select(val => (val - avg) * (val - avg)).Sum();
                return Math.Sqrt(sumOfSquaresOfDifferences / period);
            });
        }

        public static IEnumerable<double> Sma(this IEnumerable<HistoricalPrice> prices, Func<HistoricalPrice, double> selector, int period)
        {
            return Enumerable.Range(0, prices.Count()).Select(i =>
            {
                if (i < period - 1) return double.NaN;
                return prices.Skip(i - period + 1).Take(period).Select(selector).Average();
            });
        }

        public static IEnumerable<double> Sma(this IEnumerable<double> prices, Func<double, double> selector, int period)
        {
            return Enumerable.Range(0, prices.Count()).Select(i =>
            {
                if (i < period - 1) return double.NaN;
                return prices.Skip(i - period + 1).Take(period).Select(selector).Average();
            });
        }

        public static double TrueRange(this HistoricalPrice price)
        {
            return Math.Max(Convert.ToDouble(price.High - price.Low), Math.Max(Convert.ToDouble(price.High - price.Close), Convert.ToDouble(price.Close - price.Low)));
        }
    }
}
