using StockSignalScanner.Models;

namespace StockSignalScanner.Indicators
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
            List<decimal> shortEMA = MovingAverage.CalculateEMA(closePrices.ToList(), shortPeriod);
            List<decimal> longEMA = MovingAverage.CalculateEMA(closePrices.ToList(), longPeriod);

            // Calculate the MACD and signal values
            for (int i = 0; i < closePrices.Count; i++)
            {
                macdValues.Add(shortEMA[i] - longEMA[i]);

                // Add the time value
                macdTimes.Add(times[i]);
            }

            signalValues.AddRange(MovingAverage.CalculateEMA(macdValues, signalPeriod));

            // Return the MACD, signal, and time values
            return (macdValues, signalValues, macdTimes);
        }
    }
}