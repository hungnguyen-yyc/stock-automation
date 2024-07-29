using Stock.Shared.Models;
using Stock.Strategies.Parameters;

public class SwingPointParametersProvider
{
    public static SwingPointStrategyParameter GetSwingPointStrategyParameter(string ticker, Timeframe timeframe)
    {
        switch (ticker)
        {
            case "TSLA":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "AMD":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "NVDA":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "META":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "QQQ":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "SPY":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "RBLX":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
            case "V":
                return GetDefaultParameter(timeframe).Merge(new SwingPointStrategyParameter
                {
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                });
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
                NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
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