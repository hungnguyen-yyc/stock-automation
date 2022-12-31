using StockSignalScanner.Models;

namespace StockSignalScanner.Indicators
{
    public static class AverageTrueRange
    {
        public static List<decimal> Calculate(List<HistoricalPrice> prices, int period)
        {
            // Initialize a list to store the average true range (ATR) values
            var atrValues = new List<decimal>();

            // Iterate through the prices
            for (int i = 0; i < prices.Count; i++)
            {
                // Get the current price and the previous price
                var currentPrice = prices[i];
                var previousPrice = i > 0 ? prices[i - 1] : currentPrice;

                // Calculate the true range value
                decimal trueRange = TrueRange(currentPrice, previousPrice);

                // Calculate the ATR value
                decimal atrValue;
                if (i == 0)
                {
                    // For the first data point, the ATR is equal to the true range
                    atrValue = trueRange;
                }
                else
                {
                    // For subsequent data points, the ATR is calculated using an exponential moving average of the true range values
                    atrValue = (atrValues[i - 1] * (period - 1) + trueRange) / period;
                }

                // Add the ATR value to the list
                atrValues.Add(atrValue);
            }

            // Return the list of ATR values
            return atrValues;
        }

        private static decimal TrueRange(HistoricalPrice currentPrice, HistoricalPrice previousPrice)
        {
            // Calculate the maximum of the absolute difference between the high and low prices, and the absolute difference between the high and previous close prices, and the absolute difference between the low and previous close prices
            decimal maxAbsoluteDifference = Math.Max(Math.Abs(currentPrice.High - currentPrice.Low), Math.Abs(currentPrice.High - previousPrice.Close));
            maxAbsoluteDifference = Math.Max(maxAbsoluteDifference, Math.Abs(currentPrice.Low - previousPrice.Close));

            // Return the true range value
            return maxAbsoluteDifference;
        }
    }
}
