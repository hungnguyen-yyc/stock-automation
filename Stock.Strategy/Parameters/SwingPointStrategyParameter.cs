using Stock.Shared.Models;

namespace Stock.Strategies.Parameters
{
    public class SwingPointStrategyParameter : IStrategyParameter
    {
        public int NumberOfSwingPointsToLookBack { get; set; }
        public int NumberOfCandlesticksToLookBack { get; set; }
        public int NumberOfCandlesticksToSkipAfterSwingPoint { get; set; }
        public int NumberOfTouchesToDrawTrendLine { get; set; }
        public int NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint { get; set; }
        public Timeframe Timeframe { get; set; }
        public int NumberOfCandlesticksBeforeCurrentPriceToLookBack { get; set; }
        public int NumberOfCandlesticksIntersectForTopsAndBottoms { get; set; }
    }
}
