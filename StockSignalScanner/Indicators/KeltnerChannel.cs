using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    public static class KeltnerChannel
    {
        public static List<(decimal LowerChannel, decimal UpperChannel)> Calculate(List<IPrice> prices, int period, int atrPeriod)
        {
            // Calculate the average true range (ATR) values
            var atrValues = AverageTrueRange.Calculate(prices, atrPeriod);

            // Calculate the moving average values
            var movingAverages = MovingAverage.Calculate(prices, period);

            // Initialize a list to store the Keltner Channel values
            var keltnerChannels = new List<(decimal LowerChannel, decimal UpperChannel)>();

            // Iterate through the ATR and moving average values
            for (int i = 0; i < atrValues.Count; i++)
            {
                // Get the current ATR and moving average values
                decimal atrValue = atrValues[i];
                decimal movingAverage = movingAverages[i];

                // Calculate the lower and upper Keltner Channel values
                decimal lowerChannel = movingAverage - atrValue;
                decimal upperChannel = movingAverage + atrValue;

                // Add the Keltner Channel values to the list
                keltnerChannels.Add((lowerChannel, upperChannel));
            }

            // Return the list of Keltner Channel values
            return keltnerChannels;
        }
    }
}
