using Newtonsoft.Json;
using Skender.Stock.Indicators;
using Stock.Data;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies
{
    public sealed class ChainedSwingPointsStrategy : IStrategy
    {
        private const decimal OFFSET = 0.01m;
        private const string StockTradingPath = "Stock.Trading";
        private const string LevelPath = "Level";
        private const string DownTrendLinePath = "DownTrendLine";
        private const string UpTrendLinePath = "UpTrendLine";
        private readonly string _localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        public event AlertEventHandler AlertCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";
        
        public void Run(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            (Price, NumericRange)? signalPriceAtLevel = null;
            (Price, Price, Price)? signalPriceAtUpTrendLine = null;
            (Price, Price, Price)? signalPriceAtDownTrendLine = null;
            
            for (var i = ascSortedByDatePrice.Count - 15; i < ascSortedByDatePrice.Count; i++)
            {
                var runningPrices = ascSortedByDatePrice.Take(i).ToList();
                
                signalPriceAtLevel = CheckForTopBottomTouch(ticker, runningPrices, strategyParameter);
                signalPriceAtDownTrendLine = CheckForBreakingAboveDownTrendLine(ticker, runningPrices, strategyParameter);
                signalPriceAtUpTrendLine = CheckForBreakingBelowUpTrendLine(ticker, runningPrices, strategyParameter);
            }
            
            if (signalPriceAtLevel != null && signalPriceAtDownTrendLine != null)
            {
                var alert = new Alert
                {
                    Ticker = ticker,
                    Timeframe = strategyParameter.Timeframe,
                    CreatedAt = signalPriceAtLevel.Value.Item1.Date,
                    OrderPosition = OrderPosition.Long,
                    Message = $"Price {signalPriceAtLevel.Value.Item1.Close} at level {signalPriceAtLevel.Value.Item2} and breaking above downtrend line {signalPriceAtDownTrendLine.Value.Item2.DateAsString} && {signalPriceAtDownTrendLine.Value.Item3.DateAsString}",
                    PriceClosed = signalPriceAtLevel.Value.Item1.Close,
                    Strategy = nameof(ChainedSwingPointsStrategy)
                };
                AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            }
            else if (signalPriceAtLevel != null && signalPriceAtUpTrendLine != null)
            {
                var alert = new Alert
                {
                    Ticker = ticker,
                    Timeframe = strategyParameter.Timeframe,
                    CreatedAt = signalPriceAtLevel.Value.Item1.Date,
                    OrderPosition = OrderPosition.Short,
                    Message = $"Price {signalPriceAtLevel.Value.Item1.Close} at level {signalPriceAtLevel.Value.Item2.Low}-{signalPriceAtLevel.Value.Item2.High} and breaking below uptrend line {signalPriceAtUpTrendLine.Value.Item2.DateAsString} && {signalPriceAtUpTrendLine.Value.Item3.DateAsString}",
                    PriceClosed = signalPriceAtLevel.Value.Item1.Close,
                    Strategy = nameof(ChainedSwingPointsStrategy)
                };
                AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            }
        }
        
        private (Price, NumericRange)? CheckForTopBottomTouch(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            try
            {
                var secondLastPrice = ascSortedByDatePrice[ascSortedByDatePrice.Count - 2];
                var price = ascSortedByDatePrice.Last();
                var excludeLastPrice = ascSortedByDatePrice.Take(ascSortedByDatePrice.Count - 1).ToList();
                var allLevels = SwingPointAnalyzer.GetLevels(excludeLastPrice, parameter.NumberOfCandlesticksToLookBack).ToList();
                var levels = allLevels
                    .Where(x => x.Value.Count + 1 >= parameter.NumberOfCandlesticksIntersectForTopsAndBottoms) // + 1 because we need to include the key
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

                if (levelSecondLastPriceTouched.Any())
                {
                    var latestLevel = levelSecondLastPriceTouched.Last();
                    
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
                        && priceNotIntersectCenterLevelPoint)
                    {
                        return (price, centerPoint);
                        
                    }
                    else if (secondLastPriceIntersectCenterLevelPoint
                        && secondLastPrice.Low < centerPoint.Low
                        && price.Close < secondLastPrice.Close
                        && price.Close < centerPoint.Low
                        && priceIntersectSecondLastPrice
                        && priceNotIntersectCenterLevelPoint)
                    {
                        return (price, centerPoint);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking for top bottom touch for {ticker} at {parameter.Timeframe}: " + ex.Message, ex);
            }
            
        }

        private (Price, Price, Price)? CheckForBreakingAboveDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var last6MonthAction = ascSortedByDatePrice.Where(x => x.Date >= DateTime.Now.Date.AddMonths(-6)).ToList();
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var highLines = SwingPointAnalyzer.GetTrendlines(last6MonthAction, parameter, true);
            var trendingDownLines = highLines.Where(x => x.Item2.High < x.Item1.High).ToList();

            var thirdLastPriceIndex = last6MonthAction.Count - 3;
            var secondLastPriceIndex = last6MonthAction.Count - 2;
            var currentPriceIndex = last6MonthAction.Count - 1;
            var thirdLastPrice = last6MonthAction[thirdLastPriceIndex];
            var secondLastPrice = last6MonthAction[secondLastPriceIndex];
            var price = last6MonthAction.Last();
            
            foreach (var lineEnds in trendingDownLines)
            {
                var lineStart = lineEnds.Item1;
                var lineEnd = lineEnds.Item2;
                var lineStartIndex = last6MonthAction.IndexOf(lineStart);
                var lineEndIndex = last6MonthAction.IndexOf(lineEnd);
                var lineStartPoint = new Point(lineStartIndex, lineStart.High);
                var lineEndPoint = new Point(lineEndIndex, lineEnd.High);
                var line = new Line(lineStartPoint, lineEndPoint);

                var projectedYForSecondLastPriceLine = line.FindYAtX(secondLastPriceIndex);
                var projectedYForCurrentPriceLine = line.FindYAtX(currentPriceIndex);
                var projectedYForThirdLastPriceLine = line.FindYAtX(thirdLastPriceIndex);

                var secondLastHighPoint = new Point(secondLastPriceIndex, secondLastPrice.High);
                var secondLastLowPoint = new Point(secondLastPriceIndex, secondLastPrice.Low);
                var currentHighPoint = new Point(currentPriceIndex, price.High);
                var currentLowPoint = new Point(last6MonthAction.Count - 1, price.Low);

                var crossSecondLastPrice = SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(secondLastPriceIndex, projectedYForSecondLastPriceLine), secondLastLowPoint, secondLastHighPoint);
                var notCrossCurrentPrice = !SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(currentPriceIndex, projectedYForCurrentPriceLine), currentLowPoint, currentHighPoint);
                var priceAboveLine = price.Close > projectedYForCurrentPriceLine && price.Open > projectedYForCurrentPriceLine;
                var priceCloseAboveSecondLastPrice = secondLastPrice.IsGreenCandle ? price.Close > secondLastPrice.Close : price.Close > secondLastPrice.Open;
                var lastPriceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange);

                var candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine = false;
                var numberOfCandlesFromEndPointToLastThirdPrice = thirdLastPriceIndex - (lineEndIndex + 1);
                if (numberOfCandlesFromEndPointToLastThirdPrice > 0)
                {
                    var priceRangeFromEndPointToLastThirdPrice = last6MonthAction.GetRange(lineEndIndex, numberOfCandlesFromEndPointToLastThirdPrice);
                    var bodyRange = priceRangeFromEndPointToLastThirdPrice.Select(x => last6MonthAction.GetPriceBodyLineCoordinate(x));
                    candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine = bodyRange.All(x => !SwingPointAnalyzer.DoLinesIntersect(lineEndPoint, new Point(thirdLastPriceIndex, projectedYForThirdLastPriceLine), x.Start, x.End));
                }

                // checking for break above downtrend line
                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceAboveLine 
                    && priceCloseAboveSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine)
                {
                    return (price, lineEnds.Item1, lineEnds.Item2);
                }
                else
                {
                    var thirdLastPriceOverTrendLine = thirdLastPrice.Close > projectedYForThirdLastPriceLine;
                    var thirdLastPriceHigherThanSecondLastPrice = thirdLastPrice.Close > secondLastPrice.Close;
                    var priceGreaterThanSecondLastPrice = price.Close > secondLastPrice.Close;
                    var priceGreaterThanProjectedPrice = price.Close > projectedYForCurrentPriceLine;

                    // checking for rebound on downtrend line
                    if (thirdLastPriceOverTrendLine 
                        && thirdLastPriceHigherThanSecondLastPrice 
                        && priceGreaterThanSecondLastPrice 
                        && notCrossCurrentPrice 
                        && crossSecondLastPrice
                        && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine
                        && priceGreaterThanProjectedPrice)
                    {
                        return (price, lineEnds.Item1, lineEnds.Item2);
                    }
                }
            }

            return null;
        }

        private (Price, Price, Price)? CheckForBreakingBelowUpTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var last6MonthAction = ascSortedByDatePrice.Where(x => x.Date >= DateTime.Now.Date.AddMonths(-12)).ToList();
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            Tuple<Price, Price>[] lowLines = null;
            var downTrendLinePath = Path.Combine(_localAppDataPath, StockTradingPath, DownTrendLinePath, ticker, parameter.Timeframe.ToString());
            
            if (File.Exists(downTrendLinePath))
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new TupleConverter<Price, Price>());
                
                var lowLinesContent = File.ReadAllText(downTrendLinePath);
                var deserializeObject = JsonConvert.DeserializeObject<Tuple<Price, Price>[]>(lowLinesContent, settings);
                
                if (deserializeObject != null)
                {
                    lowLines = deserializeObject;
                }
                else
                {
                    lowLines = SwingPointAnalyzer.GetTrendlines(last6MonthAction, parameter, false);
                    File.WriteAllText(downTrendLinePath, JsonConvert.SerializeObject(lowLines));
                }
            }
            else
            {
                lowLines = SwingPointAnalyzer.GetTrendlines(last6MonthAction, parameter, false);
                File.WriteAllText(downTrendLinePath, JsonConvert.SerializeObject(lowLines));
            }
            
            var trendingUpLines = lowLines.Where(x => x.Item2.Low > x.Item1.Low).ToList();

            var thirdLastPriceIndex = last6MonthAction.Count - 3;
            var secondLastPriceIndex = last6MonthAction.Count - 2;
            var currentPriceIndex = last6MonthAction.Count - 1;
            var thirdLastPrice = last6MonthAction[thirdLastPriceIndex];
            var secondLastPrice = last6MonthAction[secondLastPriceIndex];
            var price = last6MonthAction.Last();
            
            foreach (var lineEnds in trendingUpLines)
            {
                var lineStart = lineEnds.Item1;
                var lineEnd = lineEnds.Item2;
                var lineStartIndex = last6MonthAction.IndexOf(lineStart);
                var lineEndIndex = last6MonthAction.IndexOf(lineEnd);
                var lineStartPoint = new Point(lineStartIndex, lineStart.Low);
                var lineEndPoint = new Point(lineEndIndex, lineEnd.Low);
                var line = new Line(lineStartPoint, lineEndPoint);

                var secondLastPointIndex = last6MonthAction.IndexOf(secondLastPrice);

                var projectedYForSecondLastPriceLine = line.FindYAtX(secondLastPriceIndex);
                var projectedYForCurrentPriceLine = line.FindYAtX(currentPriceIndex);
                var projectedYForThirdLastPriceLine = line.FindYAtX(thirdLastPriceIndex);

                var secondLastHighPoint = new Point(secondLastPointIndex, secondLastPrice.High);
                var secondLastLowPoint = new Point(secondLastPointIndex, secondLastPrice.Low);
                var currentHighPoint = new Point(currentPriceIndex, price.High);
                var currentLowPoint = new Point(currentPriceIndex, price.Low);

                /*
                 * 1. Check if second last price is crossing the line
                 * 2. Check if current price is not crossing the line meaning it is below the line
                 * 3. Check if current price is below the projected price of the line
                 */
                var crossSecondLastPrice = SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(secondLastPointIndex, projectedYForSecondLastPriceLine), secondLastLowPoint, secondLastHighPoint);
                var notCrossCurrentPrice = !SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(currentPriceIndex, projectedYForCurrentPriceLine), currentLowPoint, currentHighPoint);
                var priceBelowLine = price.Close < projectedYForCurrentPriceLine && price.Open < projectedYForCurrentPriceLine;
                var priceBelowSecondLastPrice = secondLastPrice.IsRedCandle ? price.Close < secondLastPrice.Close : price.Close < secondLastPrice.Open;
                var lastPriceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange);

                var candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine = false;
                var numberOfCandlesFromEndPointToLastThirdPrice = thirdLastPriceIndex - (lineEndIndex + 1);
                if (numberOfCandlesFromEndPointToLastThirdPrice > 0)
                {
                    var priceRangeFromEndPointToLastThirdPrice = last6MonthAction.GetRange(lineEndIndex, numberOfCandlesFromEndPointToLastThirdPrice);
                    var bodyRange = priceRangeFromEndPointToLastThirdPrice.Select(x => last6MonthAction.GetPriceBodyLineCoordinate(x));
                    candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine = bodyRange.All(x => !SwingPointAnalyzer.DoLinesIntersect(lineEndPoint, new Point(thirdLastPriceIndex, projectedYForThirdLastPriceLine), x.Start, x.End));
                }

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceBelowLine
                    && priceBelowSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine)
                {
                    return (price, lineEnds.Item1, lineEnds.Item2);
                }
                else
                {
                    var thirdLastPriceOverTrendLine = thirdLastPrice.Close < projectedYForThirdLastPriceLine;
                    var thirdLastPriceLowerThanSecondLastPrice = thirdLastPrice.Close < secondLastPrice.Close;
                    var priceLowerThanSecondLastPrice = price.Close < secondLastPrice.Close;
                    var priceLowerThanProjectedPrice = price.Close < projectedYForCurrentPriceLine;

                    if (thirdLastPriceOverTrendLine 
                        && thirdLastPriceLowerThanSecondLastPrice
                        && priceLowerThanSecondLastPrice
                        && notCrossCurrentPrice
                        && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine
                        && crossSecondLastPrice
                        && priceLowerThanProjectedPrice)
                    {
                        return (price, lineEnds.Item1, lineEnds.Item2);
                    }

                }
            }

            return null;
        }
    }
}
