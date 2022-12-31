﻿using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    public static class MovingAverage
    {
        public static List<decimal> Calculate(List<IPrice> prices, int period)
        {
            // Initialize a list to store the moving average values
            var movingAverages = new List<decimal>();

            // Iterate through the prices
            for (int i = 0; i < prices.Count; i++)
            {
                // Get the current price and the previous prices for the given period
                var currentPrice = prices[i];
                var previousPrices = prices.GetRange(Math.Max(0, i - period + 1), Math.Min(period - 1, i));

                // Calculate the moving average of the previous prices
                decimal movingAverage = previousPrices.Count > 0 ? previousPrices.Average(p => p.Close) : currentPrice.Close;

                // Add the moving average value to the list
                movingAverages.Add(movingAverage);
            }

            // Return the list of moving average values
            return movingAverages;
        }
    }

}
