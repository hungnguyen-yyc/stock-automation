using Skender.Stock.Indicators;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public class ImmediateSwingLowAndSwingPointStrategy : IStrategy
{
    private const decimal OFFSET = 0.01m;
    public string Description { get; }
    public event AlertEventHandler? AlertCreated;
    
    public void CheckForBullishEntry(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var swingPointsParameters = (SwingPointStrategyParameter)parameter;
        var swingLows = SwingPointAnalyzer.FindSwingLows(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        var price = prices.Last();
        var secondLastPrice = prices.ElementAt(prices.Count - 2);
        var excludeLastPrice = prices.Take(prices.Count - 1).ToList();
        var allLevels = SwingPointAnalyzer.GetLevels(excludeLastPrice, swingPointsParameters.NumberOfCandlesticksToLookBack).ToList();
        var levels = allLevels
            .Where(x => x.Value.Count + 1 >= swingPointsParameters.NumberOfCandlesticksIntersectForTopsAndBottoms) // + 1 because we need to include the key
            .ToList();
        
        var pivotLevels = levels.Select(x =>
        {
            var combineValuesAndKey = x.Value.Concat(new List<Price> { x.Key }).ToList();
            var averageHigh = combineValuesAndKey.Select(y => y.High).Average();
            var averageLow = combineValuesAndKey.Select(y => y.Low).Average();
            var averageVolume = combineValuesAndKey.Select(y => y.Volume).Average();
            var averageClose = combineValuesAndKey.Select(y => y.Close).Average();
            var averageOpen = combineValuesAndKey.Select(y => y.Open).Average();
            var sortedByDate = combineValuesAndKey.OrderBy(y => y.Date).ToList();
            var mostRecent = sortedByDate.Last();
            var averageOhlcPrice = new Price
            {
                Date = mostRecent.Date,
                Open = Math.Round(averageOpen, 2),
                High = Math.Round(averageHigh, 2),
                Low = Math.Round(averageLow, 2),
                Close = Math.Round(averageClose, 2),
                Volume = averageVolume
            };
            return new PivotLevel(parameter.Timeframe, ticker, averageOhlcPrice, combineValuesAndKey.Count + 1);
        }).ToList();
        
        var levelSecondLastPriceTouched = pivotLevels
            .Where(x =>
            {
                var center = x.Level.OHLC4;
                var centerOffset = center * OFFSET;
                var centerPoint = new NumericRange(center - centerOffset, center + centerOffset);
                return secondLastPrice.CandleRange.Intersect(centerPoint);
            })
            .ToList();
        
        var lastPrice = prices.Last();
        var lastSwingLow = swingLows.Last();
        var indexOfSecondLastSwingLowInPrices = prices.ToList().IndexOf(lastSwingLow);
        var indexOfLastPrice = prices.Count - 1;
        var numberOfBarsBetweenSwingLows = indexOfLastPrice - indexOfSecondLastSwingLowInPrices;
        var movingAwayFromLastSwingLow = numberOfBarsBetweenSwingLows <= swingPointsParameters.NumberOfCandlesticksToLookBack;
        var lastPriceCloseAboveLastSwingLow = lastPrice.Close > swingLows.Last().Low;
        
        if (levelSecondLastPriceTouched.Any())
        {
            var latestLevel = levelSecondLastPriceTouched.Last();
            
            var levelLow = latestLevel.Level.Low;
            var levelHigh = latestLevel.Level.High;
            var center = latestLevel.Level.OHLC4;
            var centerOffset = center * OFFSET;
            var centerPoint = new NumericRange(center - centerOffset, center + centerOffset);

            var priceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange); // to make sure current price is not too far from previous price to make sure it move gradually and healthily
            var secondLastPriceIntersectCenterLevelPoint = secondLastPrice.CandleRange.Intersect(centerPoint); // to make sure previous price touched the pivot level
            var priceNotIntersectCenterLevelPoint = !price.CandleRange.Intersect(centerPoint); // to make sure the current price is not too out of the pivot level which means it's heading toward a direction (up or down).

            if (secondLastPriceIntersectCenterLevelPoint
                && secondLastPrice.High > centerPoint.High
                && price.Close > secondLastPrice.Close
                && price.Close > centerPoint.High
                && priceIntersectSecondLastPrice
                && priceNotIntersectCenterLevelPoint
                && movingAwayFromLastSwingLow
                && lastPriceCloseAboveLastSwingLow)
            {
                var alert = new Alert
                {
                    Ticker = ticker,
                    Message = $"{ticker} Enter position at {price.Close} at {price.Date:yyyy-MM-dd HH:mm:ss} after {numberOfBarsBetweenSwingLows} bars and just crossed pivot level {center}",
                    CreatedAt = price.Date,
                    Strategy = "SwingPointsLiveTradingStrategy",
                    OrderPosition = OrderPosition.Long,
                    PositionAction = PositionAction.Open,
                    Timeframe = parameter.Timeframe
                };
                
                AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            }
        }
    }
}