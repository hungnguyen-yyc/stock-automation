using StockSignalScanner.Models;

namespace StockSignalScanner
{
    internal static class RSIIndicator
    {

        public static (List<decimal> rsiValues, List<DateTime> rsiTimes) GetRSI(IList<IPrice> prices, int period)
        {
            // Initialize lists to store the RSI and time values
            List<decimal> rsiValues = new List<decimal>();
            List<DateTime> rsiTimes = new List<DateTime>();

            // Extract the close prices and times from the Price objects
            List<decimal> closePrices = prices.Select(p => p.Close).ToList();
            List<DateTime> times = prices.Select(p => p.Date.DateTime).ToList();

            // Initialize variables to store the average gain and average loss
            decimal avgGain = 0;
            decimal avgLoss = 0;

            // Calculate the RSI values using a sliding window
            for (int i = 0; i < closePrices.Count; i++)
            {
                // Check if we have enough data to calculate the RSI value
                if (i >= period)
                {
                    // Calculate the change in price from the previous period
                    decimal change = closePrices[i] - closePrices[i - 1];

                    // Update the average gain and average loss
                    if (change > 0)
                    {
                        avgGain = (avgGain * (period - 1) + change) / period;
                        avgLoss = avgLoss * (period - 1) / period;
                    }
                    else
                    {
                        avgGain = avgGain * (period - 1) / period;
                        avgLoss = (avgLoss * (period - 1) - change) / period;
                    }

                    // Calculate the RSI value
                    rsiValues.Add(avgLoss == 0 ? 100 : 100 - (100 / (1 + (avgGain / avgLoss))));
                }
                else
                {
                    // Set the RSI value to zero until we have enough data
                    rsiValues.Add(0);
                }

                // Add the time value
                rsiTimes.Add(times[i]);
            }

            // Return the RSI and time values
            return (rsiValues, rsiTimes);
        }
    }
}