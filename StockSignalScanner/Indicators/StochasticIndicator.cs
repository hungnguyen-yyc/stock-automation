using StockSignalScanner.Models;

namespace StockSignalScanner.Indicators
{
    internal static class StochasticIndicator
    {

        public static (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) GetStochastic(IList<IPrice> prices, int period, int smoothK, int smoothD)
        {
            // Initialize lists to store the K, D, and time values
            List<decimal> kValues = new List<decimal>();
            List<decimal> dValues = new List<decimal>();
            List<DateTime> stochasticTimes = new List<DateTime>();

            // Extract the close, high, and low prices and times from the Price objects
            List<decimal> closePrices = prices.Select(p => p.Close).ToList();
            List<decimal> highPrices = prices.Select(p => p.High).ToList();
            List<decimal> lowPrices = prices.Select(p => p.Low).ToList();
            List<DateTime> times = prices.Select(p => p.Date.DateTime).ToList();

            // Calculate the K and D values
            for (int i = 0; i < closePrices.Count; i++)
            {
                // Check if we have enough data to calculate the K value
                if (i >= period - 1)
                {
                    // Calculate the minimum and maximum prices over the previous period
                    var closePrice = closePrices[i];
                    decimal minPrice = lowPrices.Skip(i - period + 1).Take(period).Min();
                    decimal maxPrice = highPrices.Skip(i - period + 1).Take(period).Max();

                    if (minPrice == maxPrice)
                    {
                        kValues.Add(0);
                    }
                    else
                    {
                        var k = 100 * (closePrice - minPrice) / (maxPrice - minPrice);
                        // Calculate the K value
                        kValues.Add(k);
                    }
                }
                else
                {
                    // Set the K and D values to zero until we have enough data
                    kValues.Add(0);
                }

                // Calculate the D value
                if (kValues.Count >= smoothD)
                {
                    // Calculate the moving average of the K values
                    var average = kValues.Skip(kValues.Count - smoothD).Take(smoothD).Average();
                    dValues.Add(average);
                }
                else
                {
                    dValues.Add(0);
                }

                // Add the time value
                stochasticTimes.Add(times[i]);
            }

            for (int i = kValues.Count - 1; i >= 0; i--)
            {
                var ave = kValues.Skip(i - smoothK + 1).Take(smoothK).Average();
                kValues[i] = ave;
            }

            for (int i = kValues.Count - 1; i >= 0; i--)
            {
                var ave = kValues.Skip(i - smoothD + 1).Take(smoothD).Average();
                dValues[i] = ave;
            }

            // Return the K, D, and time values
            return (kValues, dValues, stochasticTimes);
        }
    }
}