using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class ResistanceZone
    {
        public static ZoneState IsInResistanceZone(IList<IPrice> prices, int period, decimal margin = 0.025m)
        {
            // Check for valid input
            if (prices == null || prices.Count == 0 || period <= 0)
            {
                return new ZoneState(false, false, false, 0m, 0m);
            }

            // Get the current price
            decimal currentPrice = prices[prices.Count - 1].Close;

            // Calculate the lowest price over the past "period" number of days
            decimal highest = decimal.MinValue;
            decimal resistanceZoneHigh = highest;
            decimal resistanceZoneLow = highest;
            int start = prices.Count - period < 0 ? 0 : prices.Count - period;
            for (int i = start; i < prices.Count - 2; i++)// -2 so that we don't count the current price to the picture
            {
                highest = Math.Max(highest, prices[i].High);
            }
            resistanceZoneLow = highest * (1 - margin);
            resistanceZoneHigh = highest * (1 + margin);

            // Determine whether the current price is within the resistance zone
            bool isInresistanceZone = currentPrice <= resistanceZoneHigh && currentPrice >= resistanceZoneLow;
            bool isAboutEnterZone = currentPrice < resistanceZoneLow && currentPrice >= resistanceZoneLow * (1 - margin);

            // Determine whether the current price is about to head out of the resistance zone
            bool isAboutToLeaveresistanceZone = false;
            if (isInresistanceZone)
            {
                isAboutToLeaveresistanceZone = currentPrice <= resistanceZoneHigh * (1 - margin*3) || currentPrice >= resistanceZoneLow + (1 + margin*3);
            }

            return new ZoneState(isInresistanceZone, isAboutToLeaveresistanceZone, isAboutEnterZone, resistanceZoneHigh, resistanceZoneLow);
        }
    }
}
