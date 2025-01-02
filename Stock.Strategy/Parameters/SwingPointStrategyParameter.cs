using Newtonsoft.Json;
using Stock.Shared.Models;

namespace Stock.Strategies.Parameters
{
    public class SwingPointStrategyParameter : IStrategyParameter
    {
        public int? NumberOfSwingPointsToLookBack { get; set; }
        public int? NumberOfCandlesticksToLookBack { get; set; }
        public int? NumberOfCandlesticksToSkipAfterSwingPoint { get; set; }
        public int? NumberOfTouchesToDrawTrendLine { get; set; }
        public int? NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint { get; set; }
        public Timeframe Timeframe { get; set; }
        public int? NumberOfCandlesticksBeforeCurrentPriceToLookBack { get; set; }
        public int? NumberOfCandlesticksIntersectForTopsAndBottoms { get; set; }
        public int? NumberOfCandlesticksToLookBackForRebound { get; set; }
        public decimal? Offset { get; set; }

        public SwingPointStrategyParameter Merge(SwingPointStrategyParameter parameter)
        {
            if (parameter.Timeframe != Timeframe)
            {
                throw new Exception("Timeframe must be the same");
            }

            return new SwingPointStrategyParameter
            {
                Timeframe = parameter.Timeframe,
                NumberOfSwingPointsToLookBack = parameter.NumberOfSwingPointsToLookBack ?? NumberOfSwingPointsToLookBack,
                NumberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack ?? NumberOfCandlesticksToLookBack,
                NumberOfCandlesticksToSkipAfterSwingPoint = parameter.NumberOfCandlesticksToSkipAfterSwingPoint ?? NumberOfCandlesticksToSkipAfterSwingPoint,
                NumberOfTouchesToDrawTrendLine = parameter.NumberOfTouchesToDrawTrendLine ?? NumberOfTouchesToDrawTrendLine,
                NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = parameter.NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint ?? NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint,
                NumberOfCandlesticksBeforeCurrentPriceToLookBack = parameter.NumberOfCandlesticksBeforeCurrentPriceToLookBack ?? NumberOfCandlesticksBeforeCurrentPriceToLookBack,
                NumberOfCandlesticksIntersectForTopsAndBottoms = parameter.NumberOfCandlesticksIntersectForTopsAndBottoms ?? NumberOfCandlesticksIntersectForTopsAndBottoms,
                NumberOfCandlesticksToLookBackForRebound = parameter.NumberOfCandlesticksToLookBackForRebound ?? NumberOfCandlesticksToLookBackForRebound,
                Offset = parameter.Offset ?? Offset,
            };
        }
        
        public string ToJsonString()
        {
            var json = JsonConvert.SerializeObject(this);
            return json;
        }
    }
}
