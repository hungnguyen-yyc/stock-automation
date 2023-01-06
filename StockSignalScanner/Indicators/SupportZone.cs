using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class SupportZone
    {
        public static ZoneState IsInSupportZone(IList<IPrice> prices, int period, decimal margin = 0.05m)
        {
            // Check for valid input
            if (prices == null || prices.Count == 0 || period <= 0)
            {
                return new ZoneState(false, false, false, 0m, 0m);
            }

            // Get the current price
            decimal currentPrice = prices[prices.Count - 1].Close;

            // Calculate the lowest price over the past "period" number of days
            decimal lowest = decimal.MaxValue;
            decimal supportZoneHigh = lowest;
            decimal supportZoneLow = lowest;
            int start = prices.Count - period < 0 ? 0 : prices.Count - period;
            for (int i = start; i < prices.Count; i++)
            {
                lowest = Math.Min(lowest, prices[i].Low);
            }
            supportZoneLow = lowest * (1 - margin);
            supportZoneHigh = lowest * (1 + margin);

            // Determine whether the current price is within the support zone
            bool isInSupportZone = currentPrice <= supportZoneHigh && currentPrice >= supportZoneLow;
            bool isAboutEnterZone = currentPrice > supportZoneHigh && currentPrice <= supportZoneHigh * (1 + margin);

            // Determine whether the current price is about to head out of the support zone
            bool isAboutToLeaveSupportZone = false;
            if (isInSupportZone)
            {
                // Calculate the average of the last "period" number of prices
                decimal sum = 0;
                for (int i = start; i < prices.Count; i++)
                {
                    sum += prices[i].Close;
                }
                decimal average = sum / period;

                // If the average of the last "period" number of prices is higher than the current price, it may be a sign that the price is about to head out of the support zone
                isAboutToLeaveSupportZone = average > currentPrice;
            }

            return new ZoneState(isInSupportZone, isAboutToLeaveSupportZone, isAboutEnterZone, supportZoneHigh, supportZoneLow);
        }
    }
}
