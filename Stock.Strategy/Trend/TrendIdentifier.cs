using Stock.Shared.Models;
using Stock.Strategies.Helpers;

namespace Stock.Strategies.Trend
{
    internal class TrendIdentifier
    {
        public OveralTrend DetermineTrend(List<Price> prices, int numberOfCandlesToLookBack)
        {
            var swingHighs = SwingPointAnalyzer.FindSwingHighs(prices, numberOfCandlesToLookBack);
            var swingLows = SwingPointAnalyzer.FindSwingLows(prices, numberOfCandlesToLookBack);

            return DetermineTrend(swingHighs, swingLows, numberOfCandlesToLookBack);
        }

        private OveralTrend DetermineTrend(List<Price> swingHighs, List<Price> swingLows, int numberOfSwingPoints = 7)
        {
            var determindSwingHighTrend = DetermineSwingTrend(swingHighs.Select(x => x.High).ToList(), numberOfSwingPoints);
            var determindSwingLowTrend = DetermineSwingTrend(swingLows.Select(x => x.Low).ToList(), numberOfSwingPoints);

            return new OveralTrend(determindSwingHighTrend, determindSwingLowTrend, swingHighs, swingLows, numberOfSwingPoints);
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
