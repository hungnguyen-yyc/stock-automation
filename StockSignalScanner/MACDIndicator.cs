using StockSignalScanner.Models;

namespace StockSignalScanner
{
    internal static class MACDIndicator
    {


        public static (List<decimal> macdValues, List<decimal> signalValues, List<DateTime> macdTimes) GetMACD(IList<IPrice> prices, int shortPeriod, int longPeriod, int signalPeriod)
        {
            // Initialize lists to store the MACD, signal, and time values
            List<decimal> macdValues = new List<decimal>();
            List<decimal> signalValues = new List<decimal>();
            List<DateTime> macdTimes = new List<DateTime>();

            // Extract the close prices and times from the Price objects
            List<decimal> closePrices = prices.Select(p => p.Close).ToList();
            List<DateTime> times = prices.Select(p => p.Date.DateTime).ToList();

            // Calculate the MACD value
            List<decimal> shortEMA = CalculateEMA(closePrices.ToList(), shortPeriod);
            List<decimal> longEMA = CalculateEMA(closePrices.ToList(), longPeriod);

            // Calculate the MACD and signal values
            for (int i = 0; i < closePrices.Count; i++)
            {
                macdValues.Add(shortEMA[i] - longEMA[i]);

                // Add the time value
                macdTimes.Add(times[i]);
            }

            signalValues.AddRange(CalculateEMA(macdValues, signalPeriod));

            // Return the MACD, signal, and time values
            return (macdValues, signalValues, macdTimes);
        }


        private static List<decimal> CalculateEMA(List<decimal> src, int length)
        {
            decimal alpha = 2.0m / (length + 1);
            List<decimal> sum = new List<decimal>();

            for (int i = 0; i < src.Count; i++)
            {
                decimal previousSum = (i == 0 ? 0 : sum[i - 1]);
                sum.Add(alpha * src[i] + (1 - alpha) * previousSum);
            }

            return sum;
        }
    }
}