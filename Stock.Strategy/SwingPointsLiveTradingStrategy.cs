using Skender.Stock.Indicators;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies
{
    public class SwingPointsLiveTradingStrategy
    {
        public event AlertEventHandler AlertCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public void CheckForBreakAboveDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var highLines = SwingPointAnalyzer.GetTrendlines(ascSortedByDatePrice, parameter, true);
            var trendingDownLines = highLines.Where(x => x.Item2.High < x.Item1.High).ToList();

            // consolidate lines so that we only have 1 line per trend
            var consolidatedLines = new List<Tuple<Price, Price>>();
            foreach (var line in consolidatedLines)
            {
                var lineStart = line.Item1;
                var lineEnd = line.Item2;
                var containtsLineStart = consolidatedLines.Any(x => x.Item1 == lineStart);
                if (containtsLineStart)
                {
                    var existingLine = consolidatedLines.First(x => x.Item1 == lineStart);
                    if (existingLine.Item2.High > lineEnd.High)
                    {
                        consolidatedLines.Remove(existingLine);
                        consolidatedLines.Add(line);
                    }
                }
                else
                {
                    consolidatedLines.Add(line);
                }
            }

            var thirdLastPriceIndex = ascSortedByDatePrice.Count - 3;
            var secondLastPriceIndex = ascSortedByDatePrice.Count - 2;
            var currentPriceIndex = ascSortedByDatePrice.Count - 1;
            var thirdLastPrice = ascSortedByDatePrice[thirdLastPriceIndex];
            var secondLastPrice = ascSortedByDatePrice[secondLastPriceIndex];
            var price = ascSortedByDatePrice.Last();

            Alert? alert = null;
            foreach (var lineEnds in trendingDownLines)
            {
                var lineStart = lineEnds.Item1;
                var lineEnd = lineEnds.Item2;
                var lineStartIndex = ascSortedByDatePrice.IndexOf(lineStart);
                var lineEndIndex = ascSortedByDatePrice.IndexOf(lineEnd);
                var lineStartPoint = new Point(lineStartIndex, lineStart.High);
                var lineEndPoint = new Point(lineEndIndex, lineEnd.High);
                var line = new Line(lineStartPoint, lineEndPoint);

                var projectedYForSecondLastPriceLine = line.FindYAtX(secondLastPriceIndex);
                var projectedYForCurrentPriceLine = line.FindYAtX(currentPriceIndex);
                var projectedYForThirdLastPriceLine = line.FindYAtX(thirdLastPriceIndex);

                var secondLastHighPoint = new Point(secondLastPriceIndex, secondLastPrice.High);
                var secondLastLowPoint = new Point(secondLastPriceIndex, secondLastPrice.Low);
                var currentHighPoint = new Point(currentPriceIndex, price.High);
                var currentLowPoint = new Point(ascSortedByDatePrice.Count - 1, price.Low);

                var crossSecondLastPrice = SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(secondLastPriceIndex, projectedYForSecondLastPriceLine), secondLastLowPoint, secondLastHighPoint);
                var notCrossCurrentPrice = !SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(currentPriceIndex, projectedYForCurrentPriceLine), currentLowPoint, currentHighPoint);
                var priceAboveLine = price.Close > projectedYForCurrentPriceLine && price.Open > projectedYForCurrentPriceLine;
                var priceCloseAboveSecondLastPrice = secondLastPrice.IsGreenCandle ? price.Close > secondLastPrice.Close : price.Close > secondLastPrice.Open;
                var lastPriceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange);
                
                var priceNotIntersectThirdLastPrice = !price.CandleRange.Intersect(thirdLastPrice.CandleRange);

                var wma9 = ascSortedByDatePrice.GetWma(9);
                var wma21 = ascSortedByDatePrice.GetWma(21);
                var pvo = ascSortedByDatePrice.GetPvo(12, 26, 9);
                var wma9CrossBelowWma21 = wma9.Last().Wma > wma21.Last().Wma;
                var pvoCheck = pvo.Last().Pvo > 0 && pvo.Last().Pvo > pvo.Last().Signal && pvo.Last().Pvo > 5;

                var bodyRangeNotIntersectTrendLine = false;
                var numberOfCandlesFromEndPointToLastThirdPrice = thirdLastPriceIndex - (lineEndIndex + 1);
                if (numberOfCandlesFromEndPointToLastThirdPrice > 0)
                {
                    var currentPricePoint = new Point(currentPriceIndex, price.Close);
                    var priceRangeFromEndPointToLastThirdPrice = ascSortedByDatePrice.GetRange(lineEndIndex, numberOfCandlesFromEndPointToLastThirdPrice);
                    var bodyRange = priceRangeFromEndPointToLastThirdPrice.Select(x => ascSortedByDatePrice.GetPriceBodyLineCoordinate(x));
                    bodyRangeNotIntersectTrendLine = bodyRange.All(x => !SwingPointAnalyzer.DoLinesIntersect(lineEndPoint, new Point(thirdLastPriceIndex, projectedYForThirdLastPriceLine), x.Start, x.End));
                }

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceAboveLine 
                    && priceCloseAboveSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && bodyRangeNotIntersectTrendLine
                    && wma9CrossBelowWma21 
                    && pvoCheck)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking above down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})",
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderType = OrderType.Long,
                        OrderAction = OrderAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                }
                else
                {
                    var thirdLastPointIndex = ascSortedByDatePrice.IndexOf(thirdLastPrice);

                    var thirdLastPriceOverTrendLine = thirdLastPrice.Close > projectedYForThirdLastPriceLine;
                    var thirdLastPriceHigherThanSecondLastPrice = thirdLastPrice.Close > secondLastPrice.Close;
                    var priceGreaterThanSecondLastPrice = price.Close > secondLastPrice.Close;
                    var priceGreaterThanProjectedPrice = price.Close > projectedYForCurrentPriceLine;

                    if (thirdLastPriceOverTrendLine 
                        && thirdLastPriceHigherThanSecondLastPrice 
                        && priceGreaterThanSecondLastPrice 
                        && notCrossCurrentPrice 
                        && crossSecondLastPrice
                        && bodyRangeNotIntersectTrendLine
                        && wma9CrossBelowWma21
                        && pvoCheck
                        && priceGreaterThanProjectedPrice)
                    {
                        alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} ({price.Date:s}) is rebounding on down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})",
                            CreatedAt = price.Date,
                            Strategy = "SwingPointsLiveTradingStrategy",
                            OrderType = OrderType.Long,
                            OrderAction = OrderAction.Open,
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

        public void CheckForBreakBelowUpTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var lowLines = SwingPointAnalyzer.GetTrendlines(ascSortedByDatePrice, parameter, false);
            var trendingUpLines = lowLines.Where(x => x.Item2.Low > x.Item1.Low).ToList();

            // consolidate lines so that we only have 1 line per trend
            var consolidatedLines = new List<Tuple<Price, Price>>();
            foreach (var line in trendingUpLines)
            {
                var lineStart = line.Item1;
                var lineEnd = line.Item2;
                var containtsLineStart = consolidatedLines.Any(x => x.Item1 == lineStart);
                if (containtsLineStart)
                {
                    var existingLine = consolidatedLines.First(x => x.Item1 == lineStart);
                    if (existingLine.Item2.Low < lineEnd.Low)
                    {
                        consolidatedLines.Remove(existingLine);
                        consolidatedLines.Add(line);
                    }
                }
                else
                {
                    consolidatedLines.Add(line);
                }
            }

            var thirdLastPriceIndex = ascSortedByDatePrice.Count - 3;
            var secondLastPriceIndex = ascSortedByDatePrice.Count - 2;
            var currentPriceIndex = ascSortedByDatePrice.Count - 1;
            var thirdLastPrice = ascSortedByDatePrice[thirdLastPriceIndex];
            var secondLastPrice = ascSortedByDatePrice[secondLastPriceIndex];
            var price = ascSortedByDatePrice.Last();

            Alert? alert = null;
            foreach (var lineEnds in trendingUpLines)
            {
                var lineStart = lineEnds.Item1;
                var lineEnd = lineEnds.Item2;
                var lineStartIndex = ascSortedByDatePrice.IndexOf(lineStart);
                var lineEndIndex = ascSortedByDatePrice.IndexOf(lineEnd);
                var lineStartPoint = new Point(lineStartIndex, lineStart.Low);
                var lineEndPoint = new Point(lineEndIndex, lineEnd.Low);
                var line = new Line(lineStartPoint, lineEndPoint);

                var secondLastPointIndex = ascSortedByDatePrice.IndexOf(secondLastPrice);

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

                var priceLessThanSecondLastPrice = price.Close < secondLastPrice.Close;
                var priceNotIntersectThirdLastPrice = !price.CandleRange.Intersect(thirdLastPrice.CandleRange);

                var wma9 = ascSortedByDatePrice.GetWma(9);
                var wma21 = ascSortedByDatePrice.GetWma(21);
                var pvo = ascSortedByDatePrice.GetPvo(12, 26, 9);
                var wma9CrossBelowWma21 = wma9.Last().Wma < wma21.Last().Wma;
                var pvoCheck = pvo.Last().Pvo > 0 && pvo.Last().Pvo > pvo.Last().Signal && pvo.Last().Pvo > 5;

                var bodyRangeNotIntersectTrendLine = false;
                var numberOfCandlesFromEndPointToLastThirdPrice = thirdLastPriceIndex - (lineEndIndex + 1);
                if (numberOfCandlesFromEndPointToLastThirdPrice > 0)
                {
                    var currentPricePoint = new Point(currentPriceIndex, price.Close);
                    var priceRangeFromEndPointToLastThirdPrice = ascSortedByDatePrice.GetRange(lineEndIndex, numberOfCandlesFromEndPointToLastThirdPrice);
                    var bodyRange = priceRangeFromEndPointToLastThirdPrice.Select(x => ascSortedByDatePrice.GetPriceBodyLineCoordinate(x));
                    bodyRangeNotIntersectTrendLine = bodyRange.All(x => !SwingPointAnalyzer.DoLinesIntersect(lineEndPoint, new Point(thirdLastPriceIndex, projectedYForThirdLastPriceLine), x.Start, x.End));
                }

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceBelowLine
                    && priceBelowSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && bodyRangeNotIntersectTrendLine
                    && wma9CrossBelowWma21 
                    && pvoCheck)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderType = OrderType.Short,
                        OrderAction = OrderAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                }
                else
                {
                    var thirdLastPointIndex = ascSortedByDatePrice.IndexOf(thirdLastPrice);

                    var thirdLastPriceOverTrendLine = thirdLastPrice.Close < projectedYForThirdLastPriceLine;
                    var thirdLastPriceLowerThanSecondLastPrice = thirdLastPrice.Close < secondLastPrice.Close;
                    var priceLowerThanSecondLastPrice = price.Close < secondLastPrice.Close;
                    var priceLowerThanProjectedPrice = price.Close < projectedYForCurrentPriceLine;

                    if (thirdLastPriceOverTrendLine 
                        && thirdLastPriceLowerThanSecondLastPrice
                        && priceLowerThanSecondLastPrice
                        && notCrossCurrentPrice
                        && bodyRangeNotIntersectTrendLine
                        && crossSecondLastPrice
                        && wma9CrossBelowWma21
                        && pvoCheck
                        && priceLowerThanProjectedPrice)
                    {
                        alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} ({price.Date:s}) is touching from below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                            CreatedAt = price.Date,
                            Strategy = "SwingPointsLiveTradingStrategy",
                            OrderType = OrderType.Short,
                            OrderAction = OrderAction.Open,
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

        protected virtual void OnAlertCreated(AlertEventArgs e)
        {
            AlertCreated?.Invoke(this, e);
        }
    }
}
