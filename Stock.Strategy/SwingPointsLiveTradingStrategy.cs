using Skender.Stock.Indicators;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;

namespace Stock.Strategies
{
    public class SwingPointsLiveTradingStrategy
    {
        public event AlertEventHandler AlertCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public void CheckForTopBottomTouch(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var numberOfCandlesticksToLookBackBeforeCurrentPrice = parameter.NumberOfCandlesticksBeforeCurrentPriceToLookBack;
            var levels = SwingPointAnalyzer.GetLevels(ascSortedByDatePrice, parameter.NumberOfCandlesticksToLookBack).Where(x => x.Value.Count > parameter.NumberOfCandlesticksIntersectForTopsAndBottoms).ToList();

            var secondLastPrice = ascSortedByDatePrice[ascSortedByDatePrice.Count - 2];
            var price = ascSortedByDatePrice.Last();

            var pvo = ascSortedByDatePrice.GetPvo(12, 26, 9);
            var pvoCheck = pvo.Last().Pvo > 0 && pvo.Last().Pvo > pvo.Last().Signal;

            /*
             * The idea of this strategy is to look back a number of candlesticks before current price and check if any of the levels
             * first we take the prices that happened before the range of candlesticks that touch the level to determine where the price was before the level was touched
             * then we want to see if that range of candlesticks touch any of the levels so that means the current price is probably the new price direction
             */
            var priceRangeBeforeSecondLastPrice = ascSortedByDatePrice.GetRange(ascSortedByDatePrice.Count - 1 - numberOfCandlesticksToLookBackBeforeCurrentPrice, numberOfCandlesticksToLookBackBeforeCurrentPrice);

            var levelPriceRangeBeforeSecondLastPriceTouched = levels.Where(x => secondLastPrice.CandleRange.Intersect(x.Key.CandleRange)).ToList();

            Alert? alert = null;

            if (levelPriceRangeBeforeSecondLastPriceTouched.Any()) 
            {
                var level = levelPriceRangeBeforeSecondLastPriceTouched.OrderByDescending(x => x.Key.Date).First().Key;

                var priceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange);
                var secondLastPriceIntersectCenterLevelPoint = secondLastPrice.CandleRange.Intersect(level.CenterPoint);
                var priceNotIntersectCenterLevelPoint = !price.CandleRange.Intersect(level.CenterPoint);

                if (price.IsGreenCandle
                    && secondLastPrice.IsGreenCandle
                    && secondLastPriceIntersectCenterLevelPoint
                    && secondLastPrice.High > level.CenterPoint.High
                    && price.Close > secondLastPrice.Close
                    && priceIntersectSecondLastPrice
                    && priceNotIntersectCenterLevelPoint)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking above {level.CenterPoint.High} ({level.Low} - {level.High} on {level.Date:s})",
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderType = OrderType.Long,
                        OrderAction = OrderAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                }
                else if (price.IsRedCandle
                    && secondLastPrice.IsRedCandle
                    && secondLastPriceIntersectCenterLevelPoint
                    && secondLastPrice.Low < level.CenterPoint.Low
                    && price.Close < secondLastPrice.Close
                    && priceIntersectSecondLastPrice
                    && priceNotIntersectCenterLevelPoint)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking below {level.CenterPoint.Low} ({level.Low} - {level.High} on {level.Date:s})",
                        CreatedAt = price.Date,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderType = OrderType.Short,
                        OrderAction = OrderAction.Open,
                        Timeframe = parameter.Timeframe
                    };
                }
            }

            if (alert != null)
            {
                OnAlertCreated(new AlertEventArgs(alert));
            }
        }

        public void CheckForBreakAboveDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var highLines = SwingPointAnalyzer.GetTrendlines(ascSortedByDatePrice, parameter, true);
            var trendingDownLines = highLines.Where(x => x.Item2.High < x.Item1.High).ToList();
            var nTops = SwingPointAnalyzer.GetNTops(ascSortedByDatePrice, parameter.NumberOfCandlesticksToLookBack);

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
                var pvoCheck = pvo.Last().Pvo > 0 && pvo.Last().Pvo > pvo.Last().Signal && pvo.Last().Histogram > 7;

                var bodyRangeNotIntersectTrendLine = false;
                var numberOfCandlesFromEndPointToLastThirdPrice = thirdLastPriceIndex - (lineEndIndex + 1);
                if (numberOfCandlesFromEndPointToLastThirdPrice > 0)
                {
                    var currentPricePoint = new Point(currentPriceIndex, price.Close);
                    var priceRangeFromEndPointToLastThirdPrice = ascSortedByDatePrice.GetRange(lineEndIndex, numberOfCandlesFromEndPointToLastThirdPrice);
                    var bodyRange = priceRangeFromEndPointToLastThirdPrice.Select(x => ascSortedByDatePrice.GetPriceBodyLineCoordinate(x));
                    bodyRangeNotIntersectTrendLine = bodyRange.All(x => !SwingPointAnalyzer.DoLinesIntersect(lineEndPoint, new Point(thirdLastPriceIndex, projectedYForThirdLastPriceLine), x.Start, x.End));
                }

                var currentPointIsNotFarAwayFromLastEndPoint = currentPriceIndex - lineEndIndex < parameter.NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint;
                var priceNotIntersectAnyTops = !nTops.Where(x => x.Value.Count >= 2).Any(x => price.CandleRange.Intersect(x.Key.CandleRange));

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceAboveLine 
                    && priceCloseAboveSecondLastPrice
                    && currentPointIsNotFarAwayFromLastEndPoint
                    && lastPriceIntersectSecondLastPrice
                    && bodyRangeNotIntersectTrendLine
                    && priceNotIntersectAnyTops
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
                        && priceNotIntersectAnyTops
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

            var nBottoms = SwingPointAnalyzer.GetNBottoms(ascSortedByDatePrice, parameter.NumberOfCandlesticksToLookBack);

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
                var pvoCheck = pvo.Last().Pvo > 0 && pvo.Last().Pvo > pvo.Last().Signal && pvo.Last().Histogram > 5;

                var bodyRangeNotIntersectTrendLine = false;
                var numberOfCandlesFromEndPointToLastThirdPrice = thirdLastPriceIndex - (lineEndIndex + 1);
                if (numberOfCandlesFromEndPointToLastThirdPrice > 0)
                {
                    var currentPricePoint = new Point(currentPriceIndex, price.Close);
                    var priceRangeFromEndPointToLastThirdPrice = ascSortedByDatePrice.GetRange(lineEndIndex, numberOfCandlesFromEndPointToLastThirdPrice);
                    var bodyRange = priceRangeFromEndPointToLastThirdPrice.Select(x => ascSortedByDatePrice.GetPriceBodyLineCoordinate(x));
                    bodyRangeNotIntersectTrendLine = bodyRange.All(x => !SwingPointAnalyzer.DoLinesIntersect(lineEndPoint, new Point(thirdLastPriceIndex, projectedYForThirdLastPriceLine), x.Start, x.End));
                }

                var currentPointIsNotFarAwayFromLastEndPoint = currentPriceIndex - lineEndIndex < parameter.NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint;
                var priceNotIntersectAnyBottoms = !nBottoms.Where(x => x.Value.Count >= 2).Any(x => price.CandleRange.Intersect(x.Key.CandleRange));

                if (crossSecondLastPrice 
                    && notCrossCurrentPrice 
                    && priceBelowLine
                    && priceBelowSecondLastPrice
                    && lastPriceIntersectSecondLastPrice
                    && currentPointIsNotFarAwayFromLastEndPoint
                    && bodyRangeNotIntersectTrendLine
                    && priceNotIntersectAnyBottoms
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
                        && priceNotIntersectAnyBottoms
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
