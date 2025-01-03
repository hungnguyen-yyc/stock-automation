﻿using Skender.Stock.Indicators;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;

namespace Stock.Strategies
{
    public sealed class SwingPointsLiveTradingHighTimeframesStrategy : ISwingPointStrategy
    {
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
                var price = ascSortedByDatePrice.Last();
                var excludeLastPrice = ascSortedByDatePrice.GetRange(0, ascSortedByDatePrice.Count - 1);
                
                var pivotPrices = SwingPointAnalyzer.GetPivotPrices(
                    excludeLastPrice, 
                    parameter.NumberOfCandlesticksToLookBack!.Value, 
                    parameter.NumberOfCandlesticksIntersectForTopsAndBottoms!.Value, 
                    parameter.Offset!.Value);
                var pivotLevels = pivotPrices.Select(x => new PivotLevel(parameter.Timeframe, ticker, x.Level, x.NumberOfSwingPointsIntersected)).ToList();
                
                PivotLevelCreated?.Invoke(this, new PivotLevelEventArgs(pivotLevels));
                
                var levelPriceBoundOffAbove = PriceBoundOffAbovePivotLevels(ascSortedByDatePrice, pivotLevels, parameter.Offset!.Value);
                var levelPriceBoundOffBelow = PriceBoundOffBelowPivotLevels(ascSortedByDatePrice, pivotLevels, parameter.Offset!.Value);
                
                if (levelPriceBoundOffAbove != null)
                {
                    var message = $"Price {price.Close:F} is bound off above pivot level {levelPriceBoundOffAbove}";
                    var rebounds = PriceReboundOffAbovePivotLevels(
                        ascSortedByDatePrice, 
                        pivotLevels, 
                        levelPriceBoundOffAbove, 
                        parameter.NumberOfCandlesticksToLookBackForRebound!.Value,
                        parameter.Offset!.Value);
                    
                    if (rebounds >= 1)
                    {
                        message = $"(Rebound ({rebounds})) Price {price.Close:F} is bound off above pivot level {levelPriceBoundOffAbove}";
                    }
                    
                    var alert = new Alert
                    {
                        Ticker = ticker,
                        Message = message,
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderPosition = OrderPosition.Long,
                        PositionAction = PositionAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                    
                    OnAlertCreated(new AlertEventArgs(alert));
                }
                else if (levelPriceBoundOffBelow != null)
                {
                    var message = $"Price {price.Close:F} is bound off below pivot level {levelPriceBoundOffBelow}";
                    var rebounds = PriceReboundOffBelowPivotLevels(
                        ascSortedByDatePrice, 
                        pivotLevels, 
                        levelPriceBoundOffBelow, 
                        parameter.NumberOfCandlesticksToLookBackForRebound!.Value,
                        parameter.Offset!.Value);
                    
                    if (rebounds >= 1)
                    {
                        message = $"(Rebound ({rebounds})) Price {price.Close:F} is bound off below pivot level {levelPriceBoundOffBelow}";
                    }
                    
                    var alert = new Alert
                    {
                        Ticker = ticker,
                        Message = message,
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderPosition = OrderPosition.Short,
                        PositionAction = PositionAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                    
                    OnAlertCreated(new AlertEventArgs(alert));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking for top bottom touch for {ticker} at {parameter.Timeframe}: " + ex.Message, ex);
            }
            
        }

        private int PriceReboundOffAbovePivotLevels(
            List<Price> ascSortedByDatePrice,
            List<PivotLevel> pivotLevels,
            NumericRange targetLevel,
            int numberOfCandlesticksToLookBack,
            decimal offset)
        {
            var numberOfRebound = 0;
            for (var i = ascSortedByDatePrice.Count - numberOfCandlesticksToLookBack; i < ascSortedByDatePrice.Count; i++)
            {
                var subList = ascSortedByDatePrice.Take(i).ToList();
                var boundOffLevel = PriceBoundOffAbovePivotLevels(subList, pivotLevels, offset);
                if (boundOffLevel != null && boundOffLevel.Intersect(targetLevel))
                {
                    numberOfRebound++;
                }
            }
            
            return numberOfRebound;
        }
        
        private int PriceReboundOffBelowPivotLevels(
            List<Price> ascSortedByDatePrice,
            List<PivotLevel> pivotLevels,
            NumericRange targetLevel,
            int numberOfCandlesticksToLookBack,
            decimal offset)
        {
            var numberOfRebound = 0;
            for (var i = ascSortedByDatePrice.Count - numberOfCandlesticksToLookBack; i < ascSortedByDatePrice.Count; i++)
            {
                var subList = ascSortedByDatePrice.Take(i).ToList();
                var boundOffLevel = PriceBoundOffBelowPivotLevels(subList, pivotLevels, offset);
                if (boundOffLevel != null && boundOffLevel.Intersect(targetLevel))
                {
                    numberOfRebound++;
                }
            }
            
            return numberOfRebound;
        }

        private NumericRange? PriceBoundOffAbovePivotLevels(List<Price> ascSortedByDatePrice, List<PivotLevel> pivotLevels, decimal offset)
        {
            var price = ascSortedByDatePrice.Last();
            var secondLastPrice = ascSortedByDatePrice[^2];
            var levelSecondLastPriceTouched = pivotLevels
                    .Where(x =>
                    {
                        var centerPoint = GetCenterPoint(x, offset);
                        return secondLastPrice.CandleRange.Intersect(centerPoint);
                    })
                    .ToList();

            if (levelSecondLastPriceTouched.Any())
            {
                var latestLevel = levelSecondLastPriceTouched.Last();
                var centerPoint = GetCenterPoint(latestLevel, offset);

                var priceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange); // to make sure current price is not too far from previous price to make sure it move gradually and healthily
                var secondLastPriceIntersectCenterLevelPoint = secondLastPrice.CandleRange.Intersect(centerPoint); // to make sure previous price touched the pivot level
                var priceNotIntersectCenterLevelPoint = !price.CandleRange.Intersect(centerPoint); // to make sure the current price is not too out of the pivot level which means it's heading toward a direction (up or down).

                if (secondLastPriceIntersectCenterLevelPoint
                    && secondLastPrice.High > centerPoint.High
                    && price.Close > centerPoint.High
                    && priceIntersectSecondLastPrice
                    && priceNotIntersectCenterLevelPoint)
                {
                    return centerPoint;
                }
            }
            
            return null;
        }
        
        private NumericRange?  PriceBoundOffBelowPivotLevels(List<Price> ascSortedByDatePrice, List<PivotLevel> pivotLevels, decimal offset)
        {
            var price = ascSortedByDatePrice.Last();
            var secondLastPrice = ascSortedByDatePrice[^2];
            var levelSecondLastPriceTouched = pivotLevels
                .Where(x =>
                {
                    var centerPoint = GetCenterPoint(x, offset);
                    return secondLastPrice.CandleRange.Intersect(centerPoint);
                })
                .ToList();

            if (levelSecondLastPriceTouched.Any())
            {
                var latestLevel = levelSecondLastPriceTouched.Last();
                var centerPoint = GetCenterPoint(latestLevel, offset);

                var priceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange); // to make sure current price is not too far from previous price to make sure it move gradually and healthily
                var secondLastPriceIntersectCenterLevelPoint = secondLastPrice.CandleRange.Intersect(centerPoint); // to make sure previous price touched the pivot level
                var priceNotIntersectCenterLevelPoint = !price.CandleRange.Intersect(centerPoint); // to make sure the current price is not too out of the pivot level which means it's heading toward a direction (up or down).

                if (secondLastPriceIntersectCenterLevelPoint
                    && secondLastPrice.Low < centerPoint.Low
                    && price.Close < centerPoint.Low
                    && priceIntersectSecondLastPrice
                    && priceNotIntersectCenterLevelPoint)
                {
                    return centerPoint;
                }
            }
            
            return null;
        }
        
        private NumericRange GetCenterPoint(PivotLevel pivotLevel, decimal offset)
        {
            var center = pivotLevel.Level.OHLC4;
            var centerOffset = center * offset;
            return new NumericRange(center - centerOffset, center + centerOffset);
        }

        public void CheckForTouchingDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var last6MonthAction = ascSortedByDatePrice.Where(x => x.Date >= DateTime.Now.Date.AddMonths(-12)).ToList();
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var highLines = SwingPointAnalyzer.GetTrendlines(
                last6MonthAction,
                parameter.NumberOfCandlesticksToLookBack!.Value,
                parameter.NumberOfTouchesToDrawTrendLine!.Value,
                parameter.NumberOfCandlesticksToSkipAfterSwingPoint!.Value, 
                true);
            var trendingDownLines = highLines.Where(x => x.Item2.High < x.Item1.High).ToList();
            var nTops = SwingPointAnalyzer.GetNTops(last6MonthAction, parameter.NumberOfCandlesticksToLookBack!.Value);

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

                var priceNotIntersectAnyTops = nTops
                    .Where(x => x.Value.Count >= 2)
                    .Where(x => price.CandleRange.Intersect(x.Key.CandleRange))
                    .ToList();

                // checking for break above down trend line
                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceAboveLine 
                    && priceCloseAboveSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine)
                {
                    var message = $"Price {price.Close} ({price.Date:s}) is breaking above down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})";
                    if (priceNotIntersectAnyTops.Any())
                    {
                        var level = priceNotIntersectAnyTops.First();
                        message = $"Price {price.Close} ({price.Date:s}) is breaking above down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s}) and intersecting with previous tops {level.Key.Close}";
                    }
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
                        && priceGreaterThanProjectedPrice)
                    {
                        var message =
                            $"Price {price.Close} ({price.Date:s}) is rebounding on down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})";
                        if (priceNotIntersectAnyTops.Any())
                        {
                            var level = priceNotIntersectAnyTops.First();
                            message = $"Price {price.Close} ({price.Date:s}) is rebounding on down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s}) and intersecting with previous tops {level.Key.Close}";
                        }
                        
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
            var lowLines = SwingPointAnalyzer.GetTrendlines(
                last6MonthAction, 
                parameter.NumberOfCandlesticksToLookBack!.Value,
                parameter.NumberOfTouchesToDrawTrendLine!.Value,
                parameter.NumberOfCandlesticksToSkipAfterSwingPoint!.Value,
                false);
            var trendingUpLines = lowLines.Where(x => x.Item2.Low > x.Item1.Low).ToList();

            var nBottoms = SwingPointAnalyzer.GetNBottoms(last6MonthAction, parameter.NumberOfCandlesticksToLookBack!.Value);

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
                
                var priceIntersectAnyBottoms = nBottoms
                    .Where(x => x.Value.Count >= 2)
                    .Where(x => price.CandleRange.Intersect(x.Key.CandleRange))
                    .ToList();

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceBelowLine
                    && priceBelowSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && candleBetweenLastEndpointToCurrentPriceNotIntersectTrendLine)
                {
                    var message = $"Price {price.Close} ({price.Date:s}) is breaking below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})";
                    if (priceIntersectAnyBottoms.Any())
                    {
                        var level = priceIntersectAnyBottoms.First();
                        message = $"Price {price.Close} ({price.Date:s}) is breaking below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s}) and intersecting with previous bottoms {level.Key.Close}";
                    }
                    
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
                        var message =
                            $"Price {price.Close} ({price.Date:s}) is touching from below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})";
                        if (priceIntersectAnyBottoms.Any())
                        {
                            var level = priceIntersectAnyBottoms.First();
                            message = $"Price {price.Close} ({price.Date:s}) is touching from below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s}) and intersecting with previous bottoms {level.Key.Close}";
                        }
                        
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

        private void OnAlertCreated(AlertEventArgs e)
        {
            AlertCreated?.Invoke(this, e);
        }
    }
}
