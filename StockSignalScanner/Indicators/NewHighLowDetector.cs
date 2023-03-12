using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class NewHighLowDetector
    {
        public List<IPrice> LowerLows { get; set; }
        public List<IPrice> HigherLows { get; set; }
        public List<IPrice> LowerHighs { get; set; }
        public List<IPrice> HigherHighs { get; set; }

        public NewHighLowDetector()
        {
            LowerLows = new List<IPrice>();
            HigherLows = new List<IPrice>();
            LowerHighs = new List<IPrice>();
            HigherHighs = new List<IPrice>();
        }

        public void AnalyzePrices(IList<IPrice> prices)
        {
            IPrice previousPrice = null;
            IPrice currentPrice;
            IPrice nextPrice;

            for (int i = 0; i < prices.Count; i++)
            {
                currentPrice = prices[i];
                nextPrice = i + 1 < prices.Count ? prices[i + 1] : null;

                if (previousPrice != null)
                {
                    if (currentPrice.Low < previousPrice.Low)
                    {
                        if (nextPrice != null && currentPrice.Low < nextPrice.Low)
                        {
                            LowerLows.Add(currentPrice);
                        }
                    }
                    else if (currentPrice.Low > previousPrice.Low)
                    {
                        if (nextPrice != null && currentPrice.Low > nextPrice.Low)
                        {
                            HigherLows.Add(currentPrice);
                        }
                    }

                    if (currentPrice.High < previousPrice.High)
                    {
                        if (nextPrice != null && currentPrice.High < nextPrice.High)
                        {
                            LowerHighs.Add(currentPrice);
                        }
                    }
                    else if (currentPrice.High > previousPrice.High)
                    {
                        if (nextPrice != null && currentPrice.High > nextPrice.High)
                        {
                            HigherHighs.Add(currentPrice);
                        }
                    }
                }

                previousPrice = currentPrice;
            }
        }

        public Trend GetTrendReversal(IList<IPrice> prices)
        {
            AnalyzePrices(prices);
            Trend currentTrend = Trend.Sideways;

            if (HigherHighs.Count > LowerHighs.Count && HigherLows.Count > LowerLows.Count)
            {
                currentTrend = Trend.Uptrend;
            }
            else if (LowerHighs.Count > HigherHighs.Count && LowerLows.Count > HigherLows.Count)
            {
                currentTrend = Trend.Downtrend;
            }

            return currentTrend;
        }
    }
}
