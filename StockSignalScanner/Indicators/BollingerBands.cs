using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    public static class BollingerBands
    {
        public static List<(decimal LowerBand, decimal UpperBand)> Calculate(List<IPrice> prices, int period, decimal standardDeviation)
        {
            // Calculate the moving average values
            var movingAverages = MovingAverage.CalculateSMA(prices, period);

            // Initialize a list to store the Bollinger Band values
            var bollingerBands = new List<(decimal LowerBand, decimal UpperBand)>();

            // Iterate through the moving average values
            for (int i = 0; i < movingAverages.Count; i++)
            {
                // Get the current moving average value and the previous prices for the given period
                decimal movingAverage = movingAverages[i];
                var previousPrices = prices.GetRange(Math.Max(0, i - period + 1), Math.Min(period - 1, i));

                // Calculate the standard deviation of the previous prices
                decimal standardDeviationValue = previousPrices.Count > 0 ? StandardDeviation(previousPrices.Select(p => p.Close)) : 0;

                // Calculate the lower and upper Bollinger Band values
                decimal lowerBand = movingAverage - standardDeviation * standardDeviationValue;
                decimal upperBand = movingAverage + standardDeviation * standardDeviationValue;

                // Add the Bollinger Band values to the list
                bollingerBands.Add((lowerBand, upperBand));
            }

            // Return the list of Bollinger Band values
            return bollingerBands;
        }

        private static decimal StandardDeviation(IEnumerable<decimal> values)
        {
            // Calculate the mean of the values
            double mean = (double)values.Average();

            // Calculate the sum of the squared differences from the mean
            double sumOfSquaredDifferences = values.Sum(v => Math.Pow(Convert.ToDouble(v) - mean, 2));

            // Return the standard deviation of the values
            return (decimal)Math.Sqrt(sumOfSquaredDifferences / values.Count());
        }
    }
}

