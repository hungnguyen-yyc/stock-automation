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

        public SwingPointStrategyParameter Merge(SwingPointStrategyParameter parameter)
        {
            if (parameter.Timeframe != Timeframe)
            {
                throw new Exception("Timeframe must be the same");
            }

            return new SwingPointStrategyParameter
            {
                NumberOfSwingPointsToLookBack = parameter.NumberOfSwingPointsToLookBack == 0 ? NumberOfSwingPointsToLookBack : parameter.NumberOfSwingPointsToLookBack,
                NumberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack == 0 ? NumberOfCandlesticksToLookBack : parameter.NumberOfCandlesticksToLookBack,
                NumberOfCandlesticksToSkipAfterSwingPoint = parameter.NumberOfCandlesticksToSkipAfterSwingPoint == 0 ? NumberOfCandlesticksToSkipAfterSwingPoint : parameter.NumberOfCandlesticksToSkipAfterSwingPoint,
                NumberOfTouchesToDrawTrendLine = parameter.NumberOfTouchesToDrawTrendLine == 0 ? NumberOfTouchesToDrawTrendLine : parameter.NumberOfTouchesToDrawTrendLine,
                NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = parameter.NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint == 0 ? NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint : parameter.NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint,
                NumberOfCandlesticksBeforeCurrentPriceToLookBack = parameter.NumberOfCandlesticksBeforeCurrentPriceToLookBack == 0 ? NumberOfCandlesticksBeforeCurrentPriceToLookBack : parameter.NumberOfCandlesticksBeforeCurrentPriceToLookBack,
                NumberOfCandlesticksIntersectForTopsAndBottoms = parameter.NumberOfCandlesticksIntersectForTopsAndBottoms == 0 ? NumberOfCandlesticksIntersectForTopsAndBottoms : parameter.NumberOfCandlesticksIntersectForTopsAndBottoms
            };
        }
    }
}
