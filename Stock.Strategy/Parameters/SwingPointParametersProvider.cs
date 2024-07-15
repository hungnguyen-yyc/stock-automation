using Stock.Shared.Models;
using Stock.Strategies.Parameters;

public class SwingPointParametersProvider
{
    public static SwingPointStrategyParameter GetSwingPointStrategyParameter(string ticker, Timeframe timeframe)
    {
        switch (ticker)
        {
            case "TSLA":
                return new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                }.Merge(GetDefaultParameter(timeframe));
            case "AMD":
                return new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                }.Merge(GetDefaultParameter(timeframe));
            case "NVDA":
                return new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                }.Merge(GetDefaultParameter(timeframe));
            case "META":
                return new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                }.Merge(GetDefaultParameter(timeframe));
            case "QQQ":
                return new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                }.Merge(GetDefaultParameter(timeframe));
            case "SPY":
                return new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                }.Merge(GetDefaultParameter(timeframe));
            default:
                return GetDefaultParameter(timeframe);
        }
    }

    private static SwingPointStrategyParameter GetDefaultParameter(Timeframe timeframe)
    {
        if (timeframe == Timeframe.Daily)
        {
            return new SwingPointStrategyParameter
            {
                NumberOfCandlesticksToLookBack = 7,
                Timeframe = timeframe,
                NumberOfCandlesticksIntersectForTopsAndBottoms = 3,
                NumberOfSwingPointsToLookBack = 2,
                NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                NumberOfTouchesToDrawTrendLine = 2,
                NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
                NumberOfCandlesticksBeforeCurrentPriceToLookBack = 7,
            };
        }
        
        return new SwingPointStrategyParameter
        {
            NumberOfCandlesticksToLookBack = 12,
            Timeframe = timeframe,
            NumberOfCandlesticksIntersectForTopsAndBottoms = 3,

            NumberOfSwingPointsToLookBack = 2,
            NumberOfCandlesticksToSkipAfterSwingPoint = 2,
            NumberOfTouchesToDrawTrendLine = 2,
            NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
            NumberOfCandlesticksBeforeCurrentPriceToLookBack = 7,
        };
    }
}