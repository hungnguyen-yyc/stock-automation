using Stock.Shared.Models;

namespace Stock.Strategies.Trend
{
    internal class TrendIdentifier
    {
        public List<Price> FindSwingLows(List<Price> prices, int numberOfCandlesToLookBack)
        {
            List<Price> swingLows = new List<Price>();

            for (int i = numberOfCandlesToLookBack; i < prices.Count; i++)
            {
                var currentPrice = prices[i];

                bool isSwingLow = true;
                var innerRange = i < prices.Count - numberOfCandlesToLookBack ? i + numberOfCandlesToLookBack : prices.Count - 1;

                for (int j = i - numberOfCandlesToLookBack; j <= innerRange; j++)
                {
                    if (j == i)
                        continue;

                    var price = prices[j];

                    if (price.Low <= currentPrice.Low)
                    {
                        isSwingLow = false;
                        break;
                    }
                }

                if (isSwingLow)
                {
                    swingLows.Add(currentPrice);
                }
            }

            return swingLows;
        }

        public List<Price> FindSwingHighs(List<Price> prices, int numberOfCandlesToLookBack)
        {
            var swingHighs = new List<Price>();

            for (int i = numberOfCandlesToLookBack; i < prices.Count; i++)
            {
                var currentPrice = prices[i];

                bool isSwingHigh = true;
                var innerRange = i < prices.Count - numberOfCandlesToLookBack ? i + numberOfCandlesToLookBack : prices.Count - 1;

                for (int j = i - numberOfCandlesToLookBack; j <= innerRange; j++)
                {
                    if (j == i)
                        continue;

                    var price = prices[j];

                    if (price.High >= currentPrice.High)
                    {
                        isSwingHigh = false;
                        break;
                    }
                }

                if (isSwingHigh)
                {
                    swingHighs.Add(currentPrice);
                }
            }

            return swingHighs;
        }

        public OverallTrend DetermineTrend(List<Price> prices, int numberOfCandlesToLookBack)
        {
            var swingHighs = FindSwingHighs(prices, numberOfCandlesToLookBack);
            var swingLows = FindSwingLows(prices, numberOfCandlesToLookBack);

            return DetermineTrend(swingHighs, swingLows, numberOfCandlesToLookBack);
        }

        private OverallTrend DetermineTrend(List<Price> swingHighs, List<Price> swingLows, int numberOfSwingPoints = 7)
        {
            var determindSwingHighTrend = DetermineSwingTrend(swingHighs.Select(x => x.High).ToList(), numberOfSwingPoints);
            var determindSwingLowTrend = DetermineSwingTrend(swingLows.Select(x => x.Low).ToList(), numberOfSwingPoints);

            return new OverallTrend(determindSwingHighTrend, determindSwingLowTrend, swingHighs, swingLows, numberOfSwingPoints);
        }

        public TrendDirection DetermineSwingTrend(List<decimal> swings, int numSwingPoints)
        {
            if (swings.Count < numSwingPoints)
            {
                return TrendDirection.Unknown;
            }

            swings = swings.Skip(swings.Count - numSwingPoints).ToList();
            var trend = TrendDirection.Unknown;

            for (var i = 1; i < swings.Count; i++)
            {
                var currentSwing = swings[i];
                var previousSwing = swings[i - 1];
                if (currentSwing > previousSwing)
                {
                    trend = trend switch
                    {
                        TrendDirection.Downtrend => TrendDirection.ReversalToUptrend,
                        TrendDirection.ReversalToDowntrend => TrendDirection.ReversalToUptrend,
                        TrendDirection.ReversalToUptrend => TrendDirection.Uptrend,
                        TrendDirection.Uptrend => TrendDirection.Uptrend,
                        TrendDirection.Unknown => TrendDirection.Uptrend,
                        _ => TrendDirection.Uptrend,
                    };
                }
                else if (currentSwing < previousSwing)
                {
                    trend = trend switch
                    {
                        TrendDirection.Uptrend => TrendDirection.ReversalToDowntrend,
                        TrendDirection.ReversalToUptrend => TrendDirection.ReversalToDowntrend,
                        TrendDirection.ReversalToDowntrend => TrendDirection.Downtrend,
                        TrendDirection.Downtrend => TrendDirection.Downtrend,
                        TrendDirection.Unknown => TrendDirection.Downtrend,
                        _ => TrendDirection.Downtrend,
                    };
                }
            }

            return trend;
        }
    }
}
