using Skender.Stock.Indicators;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;

namespace Stock.Strategies
{
    public sealed class SwingPointsLiveTradingHighTimeframesStrategy : ISwingPointStrategy
    {
        private const decimal OFFSET = 0.01m;
        private readonly VolumeCheckingHelper _volumeCheckingHelper;
        
        public SwingPointsLiveTradingHighTimeframesStrategy()
        {
            _volumeCheckingHelper = new VolumeCheckingHelper();
        }
        
        public event AlertEventHandler AlertCreated;
        
        public event TrendLineEventHandler TrendLineCreated;
        
        public event PivotLevelEventHandler PivotLevelCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public void CheckForTopBottomTouch(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            try
            {
                var secondLastPrice = ascSortedByDatePrice[ascSortedByDatePrice.Count - 2];
                var price = ascSortedByDatePrice.Last();
                var excludeLastPrice = ascSortedByDatePrice.GetRange(0, ascSortedByDatePrice.Count - 1);
                var allLevels = SwingPointAnalyzer.GetLevels(excludeLastPrice, parameter.NumberOfCandlesticksToLookBack).ToList();
                var levels = allLevels
                    .Where(x => x.Value.Count + 1 >= parameter.NumberOfCandlesticksIntersectForTopsAndBottoms) // + 1 because we need to include the key
                    .ToList();
                var atr = ascSortedByDatePrice.GetAtr(14);
                
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
                
                PivotLevelCreated?.Invoke(this, new PivotLevelEventArgs(pivotLevels));
                
                var hmVolumeCheck = _volumeCheckingHelper.CheckHeatmapVolume(ascSortedByDatePrice, parameter);
                var isValidCandleForLong = price.IsGreenCandle && price.IsContentCandle;
                var isValidCandleForShort = price.IsRedCandle && price.IsContentCandle;

                var levelSecondLastPriceTouched = pivotLevels
                    .Where(x =>
                    {
                        var center = x.Level.OHLC4;
                        var centerOffset = center * OFFSET;
                        var centerPoint = new NumericRange(center - centerOffset, center + centerOffset);
                        return secondLastPrice.CandleRange.Intersect(centerPoint);
                    })
                    .ToList();

                Alert? alert = null;

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
                        && priceNotIntersectCenterLevelPoint)
                    {
                        var message = $"Price {price.Close} ({price.Date:s}) > {centerPoint.High} ({levelLow} - {levelHigh})";

                        if (hmVolumeCheck && isValidCandleForLong)
                        {
                            alert = new TopNBottomStrategyAlert
                            {
                                Ticker = ticker,
                                Message = "(O)" + message,
                                CreatedAt = price.Date,
                                Strategy = "SwingPointsLiveTradingStrategy",
                                OrderPosition = OrderPosition.Long,
                                PositionAction = PositionAction.Open,
                                Timeframe = parameter.Timeframe,
                                High = secondLastPrice.Low,
                                Center = center,
                                PriceClosed = price.Close,
                                ATR = (decimal)atr.Last().Atr,
                            };
                        }
                        else
                        {
                            alert = new Alert
                            {
                                Ticker = ticker,
                                Message = message,
                                CreatedAt = price.Date,
                                Strategy = "SwingPointsLiveTradingStrategy",
                                OrderPosition = OrderPosition.Long,
                                PositionAction = PositionAction.Open,
                                Timeframe = parameter.Timeframe
                            };
                        }
                        
                    }
                    else if (secondLastPriceIntersectCenterLevelPoint
                        && secondLastPrice.Low < centerPoint.Low
                        && price.Close < secondLastPrice.Close
                        && price.Close < centerPoint.Low
                        && priceIntersectSecondLastPrice
                        && priceNotIntersectCenterLevelPoint)
                    {
                        var message = $"Price {price.Close} ({price.Date:s}) < {centerPoint.Low} ({levelLow} - {levelHigh})";

                        if (hmVolumeCheck && isValidCandleForShort)
                        {
                            alert = new TopNBottomStrategyAlert
                            {
                                Ticker = ticker,
                                Message = "(O)" + message,
                                CreatedAt = price.Date,
                                Strategy = "SwingPointsLiveTradingStrategy",
                                OrderPosition = OrderPosition.Short,
                                PositionAction = PositionAction.Open,
                                Timeframe = parameter.Timeframe,
                                High = secondLastPrice.High,
                                Center = center,
                                PriceClosed = price.Close,
                                ATR = (decimal)atr.Last().Atr
                            };
                        }
                        else
                        {
                            alert = new Alert
                            {
                                Ticker = ticker,
                                Message = message,
                                CreatedAt = price.Date,
                                Strategy = "SwingPointsLiveTradingStrategy",
                                OrderPosition = OrderPosition.Short,
                                PositionAction = PositionAction.Open,
                                Timeframe = parameter.Timeframe
                            };
                        }
                    }
                }

                if (alert != null)
                {
                    OnAlertCreated(new AlertEventArgs(alert));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking for top bottom touch for {ticker} at {parameter.Timeframe}: " + ex.Message, ex);
            }
            
        }

        public void CheckForTouchingDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var last6MonthAction = ascSortedByDatePrice.Where(x => x.Date >= DateTime.Now.Date.AddMonths(-6)).ToList();
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var highLines = SwingPointAnalyzer.GetTrendlines(last6MonthAction, parameter, true);
            var trendingDownLines = highLines.Where(x => x.Item2.High < x.Item1.High).ToList();
            var nTops = SwingPointAnalyzer.GetNTops(last6MonthAction, parameter.NumberOfCandlesticksToLookBack);

            var thirdLastPriceIndex = last6MonthAction.Count - 3;
            var secondLastPriceIndex = last6MonthAction.Count - 2;
            var currentPriceIndex = last6MonthAction.Count - 1;
            var thirdLastPrice = last6MonthAction[thirdLastPriceIndex];
            var secondLastPrice = last6MonthAction[secondLastPriceIndex];
            var price = last6MonthAction.Last();

            Alert? alert = null;
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

                var priceNotIntersectAnyTops = !nTops.Where(x => x.Value.Count >= 2).Any(x => price.CandleRange.Intersect(x.Key.CandleRange));

                // checking for break above down trend line
                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceAboveLine 
                    && priceCloseAboveSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine
                    && priceNotIntersectAnyTops)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking above down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})",
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderPosition = OrderPosition.Long,
                        PositionAction = PositionAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                }
                else
                {
                    var thirdLastPriceOverTrendLine = thirdLastPrice.Close > projectedYForThirdLastPriceLine;
                    var thirdLastPriceHigherThanSecondLastPrice = thirdLastPrice.Close > secondLastPrice.Close;
                    var priceGreaterThanSecondLastPrice = price.Close > secondLastPrice.Close;
                    var priceGreaterThanProjectedPrice = price.Close > projectedYForCurrentPriceLine;

                    // checking for rebound on down trend line
                    if (thirdLastPriceOverTrendLine 
                        && thirdLastPriceHigherThanSecondLastPrice 
                        && priceGreaterThanSecondLastPrice 
                        && notCrossCurrentPrice 
                        && crossSecondLastPrice
                        && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine
                        && priceNotIntersectAnyTops
                        && priceGreaterThanProjectedPrice)
                    {
                        alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} ({price.Date:s}) is rebounding on down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})",
                            CreatedAt = price.Date,
                            Strategy = "SwingPointsLiveTradingStrategy",
                            OrderPosition = OrderPosition.Long,
                            PositionAction = PositionAction.Open,
                            Timeframe = parameter.Timeframe
                        };
                    }
                }
            }

            if (alert != null)
            {
                OnAlertCreated(new AlertEventArgs(alert));
            }
        }

        public void CheckForTouchingUpTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var last6MonthAction = ascSortedByDatePrice.Where(x => x.Date >= DateTime.Now.Date.AddMonths(-12)).ToList();
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var lowLines = SwingPointAnalyzer.GetTrendlines(last6MonthAction, parameter, false);
            var trendingUpLines = lowLines.Where(x => x.Item2.Low > x.Item1.Low).ToList();

            var nBottoms = SwingPointAnalyzer.GetNBottoms(last6MonthAction, parameter.NumberOfCandlesticksToLookBack);

            var thirdLastPriceIndex = last6MonthAction.Count - 3;
            var secondLastPriceIndex = last6MonthAction.Count - 2;
            var currentPriceIndex = last6MonthAction.Count - 1;
            var thirdLastPrice = last6MonthAction[thirdLastPriceIndex];
            var secondLastPrice = last6MonthAction[secondLastPriceIndex];
            var price = last6MonthAction.Last();

            Alert? alert = null;
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
                
                var priceNotIntersectAnyBottoms = !nBottoms.Where(x => x.Value.Count >= 2).Any(x => price.CandleRange.Intersect(x.Key.CandleRange));

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceBelowLine
                    && priceBelowSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine
                    && priceNotIntersectAnyBottoms)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderPosition = OrderPosition.Short,
                        PositionAction = PositionAction.Open,
                        Timeframe = parameter.Timeframe
                    };
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
                        && priceNotIntersectAnyBottoms
                        && crossSecondLastPrice
                        && priceLowerThanProjectedPrice)
                    {
                        alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} ({price.Date:s}) is touching from below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                            CreatedAt = price.Date,
                            Strategy = "SwingPointsLiveTradingStrategy",
                            OrderPosition = OrderPosition.Short,
                            PositionAction = PositionAction.Open,
                            Timeframe = parameter.Timeframe
                        };
                    }

                }
            }

            if (alert != null)
            {
                OnAlertCreated(new AlertEventArgs(alert));
            }
        }

        private void OnAlertCreated(AlertEventArgs e)
        {
            AlertCreated?.Invoke(this, e);
        }
    }
}
