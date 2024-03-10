using Stock.Shared.Models;
using static Stock.Strategies.Trend.TrendIdentifier;

namespace Stock.Strategies.Trend
{
    public class OveralTrend
    {
        public OveralTrend(TrendDirection swingHighTrend, TrendDirection swingLowTrend, List<Price> swingHighs, List<Price> swingLows, int numberOfSwingPoints = 7)
        {
            SwingHighTrend = swingHighTrend;
            SwingLowTrend = swingLowTrend;
            SwingHighs = swingHighs;
            SwingLows = swingLows;
            NumberOfSwingPoints = numberOfSwingPoints;
        }

        public TrendDirection SwingHighTrend { get; }
        public TrendDirection SwingLowTrend { get; }
        public List<Price> SwingHighs { get; }
        public List<Price> SwingLows { get; }
        public int NumberOfSwingPoints { get; }

        public TrendDirection Trend => DetermineOverallTrend();

        private TrendDirection DetermineOverallTrend()
        {
            // check for uptrend
            if (SwingHighTrend == TrendDirection.Uptrend && SwingLowTrend == TrendDirection.Uptrend)
            {
                return TrendDirection.Uptrend;
            }
            else if (SwingHighTrend == TrendDirection.Uptrend && SwingLowTrend == TrendDirection.Downtrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.Uptrend && SwingLowTrend == TrendDirection.ReversalToDowntrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.Uptrend && SwingLowTrend == TrendDirection.ReversalToUptrend)
            {
                return TrendDirection.Uptrend;
            }
            else if (SwingHighTrend == TrendDirection.Uptrend && SwingLowTrend == TrendDirection.Unknown)
            {
                return TrendDirection.Uptrend;
            }
            // check for downtrend
            else if (SwingHighTrend == TrendDirection.Downtrend && SwingLowTrend == TrendDirection.Uptrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.Downtrend && SwingLowTrend == TrendDirection.Downtrend)
            {
                return TrendDirection.Downtrend;
            }
            else if (SwingHighTrend == TrendDirection.Downtrend && SwingLowTrend == TrendDirection.ReversalToDowntrend)
            {
                return TrendDirection.Downtrend;
            }
            else if (SwingHighTrend == TrendDirection.Downtrend && SwingLowTrend == TrendDirection.ReversalToUptrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.Downtrend && SwingLowTrend == TrendDirection.Unknown)
            {
                return TrendDirection.Downtrend;
            }
            // check for reversal uptrend
            else if (SwingHighTrend == TrendDirection.ReversalToUptrend && SwingLowTrend == TrendDirection.Uptrend)
            {
                return TrendDirection.Uptrend;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToUptrend && SwingLowTrend == TrendDirection.Downtrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToUptrend && SwingLowTrend == TrendDirection.ReversalToDowntrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToUptrend && SwingLowTrend == TrendDirection.ReversalToUptrend)
            {
                return TrendDirection.Uptrend;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToUptrend && SwingLowTrend == TrendDirection.Unknown)
            {
                return TrendDirection.ReversalToUptrend;
            }
            // check for reversal downtrend
            else if (SwingHighTrend == TrendDirection.ReversalToDowntrend && SwingLowTrend == TrendDirection.Uptrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToDowntrend && SwingLowTrend == TrendDirection.Downtrend)
            {
                return TrendDirection.Downtrend;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToDowntrend && SwingLowTrend == TrendDirection.ReversalToDowntrend)
            {
                return TrendDirection.Downtrend;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToDowntrend && SwingLowTrend == TrendDirection.ReversalToUptrend)
            {
                return TrendDirection.Unknown;
            }
            else if (SwingHighTrend == TrendDirection.ReversalToDowntrend && SwingLowTrend == TrendDirection.Unknown)
            {
                return TrendDirection.ReversalToDowntrend;
            }
            // check for unknown
            else if (SwingHighTrend == TrendDirection.Unknown && SwingLowTrend == TrendDirection.Uptrend)
            {
                return TrendDirection.Uptrend;
            }
            else if (SwingHighTrend == TrendDirection.Unknown && SwingLowTrend == TrendDirection.Downtrend)
            {
                return TrendDirection.Downtrend;
            }
            else if (SwingHighTrend == TrendDirection.Unknown && SwingLowTrend == TrendDirection.ReversalToDowntrend)
            {
                return TrendDirection.ReversalToDowntrend;
            }
            else if (SwingHighTrend == TrendDirection.Unknown && SwingLowTrend == TrendDirection.ReversalToUptrend)
            {
                return TrendDirection.ReversalToUptrend;
            }
            else if (SwingHighTrend == TrendDirection.Unknown && SwingLowTrend == TrendDirection.Unknown)
            {
                return TrendDirection.Unknown;
            }
            else
            {
                return TrendDirection.Unknown;
            }
        }
    }
}
