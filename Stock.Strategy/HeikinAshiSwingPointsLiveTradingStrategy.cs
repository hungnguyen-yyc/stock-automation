using Skender.Stock.Indicators;
using Stock.Shared.Extensions;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;

namespace Stock.Strategies
{
    public class HeikinAshiSwingPointsLiveTradingStrategy: ISwingPointStrategy
    {
        public event AlertEventHandler AlertCreated;
        public event TrendLineEventHandler TrendLineCreated;
        public event PivotLevelEventHandler PivotLevelCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public void CheckForTopBottomTouch(string ticker, List<Price> prices, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            try
            {
                var secondLastPrice = prices[prices.Count - 2];
                var price = prices.Last();

                var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
                var numberOfCandlesticksToLookBackBeforeCurrentPrice = parameter.NumberOfCandlesticksBeforeCurrentPriceToLookBack;

                var heikinAshiFirstTransformation = prices
                    .GetHeikinAshi()
                    .Select(s => new Price
                    {
                        Low = s.Low,
                        High = s.High,
                        Open = s.Open,
                        Close = s.Close,
                        Date = s.Date,
                        Volume = s.Volume
                    }).ToList();

                var heikinAshiPrices = heikinAshiFirstTransformation.GetHeikinAshi().Select(s => new Price
                {
                    Low = s.Low,
                    High = s.High,
                    Open = s.Open,
                    Close = s.Close,
                    Date = s.Date,
                    Volume = s.Volume
                }).ToList();
                var levels = SwingPointAnalyzer.GetLevels(heikinAshiPrices, parameter.NumberOfCandlesticksToLookBack)
                    .Where(x => x.Value.Count + 1 >= parameter.NumberOfCandlesticksIntersectForTopsAndBottoms) // + 1 because we need to include the key
                    .ToList();

                // remove levels that are too close to each other
                var levelsToRemove = new List<Price>();
                foreach (var level in levels)
                {
                    var levelLow = level.Key.Low;
                    var levelHigh = level.Key.High;
                    var levelCenter = (levelLow + levelHigh) / 2;
                    var levelCenterOffset = levelCenter * (decimal)0.005;
                    var levelCenterPoint = new NumericRange(levelCenter - levelCenterOffset, levelCenter + levelCenterOffset);

                    var levelsIntersected = levels.Where(x => x.Key.CandleRange.Intersect(levelCenterPoint));
                    if (levelsIntersected.Count() > 1)
                    {
                        var levelsIntersectedLow = levelsIntersected.Select(x => x.Key.Low).Min();
                        var levelsIntersectedHigh = levelsIntersected.Select(x => x.Key.High).Max();
                        var levelsIntersectedCenter = (levelsIntersectedLow + levelsIntersectedHigh) / 2;
                        var levelsIntersectedCenterPoint = new NumericRange(levelsIntersectedCenter - levelCenterOffset, levelsIntersectedCenter + levelCenterOffset);

                        var levelsIntersectedWithCurrentLevel = levelsIntersected.Where(x => x.Key.CandleRange.Intersect(levelsIntersectedCenterPoint));
                        if (levelsIntersectedWithCurrentLevel.Count() > 1)
                        {
                            levelsToRemove.Add(level.Key);
                        }
                    }
                }

                foreach (var levelToRemove in levelsToRemove)
                {
                    foreach (var level in levels.Where(x => x.Key.Equals(levelToRemove)).ToList())
                    {
                        levels.Remove(level);
                    }
                }

                // remove any prices from levels key or value that has the same date as the current price
                var tempLevels = levels.ToList();
                foreach (var level in tempLevels)
                {
                    var levelKey = level.Key;
                    if (levelKey.Date.Date == price.Date.Date)
                    {
                        levels.Remove(level);
                    }
                    else
                    {
                        var levelValue = level.Value;
                        foreach (var levelPrice in levelValue)
                        {
                            if (levelPrice.Date.Date == price.Date.Date)
                            {
                                levelValue.Remove(levelPrice);
                            }
                        }
                    }
                }

                var midLines = levels.Select(x => new { mid = (x.Value.Select(y => y.High).Max()) + (x.Value.Select(y => y.Low).Min()) / 2, count = x.Value.Count()})
                    .OrderByDescending(x => x.mid)
                    .ToList();

                var hmVolumes = prices.GetHeatmapVolume(21, 21);
                var hmVolume = hmVolumes.Last().Volume;
                var hmvThresholdStatus = hmVolumes.Last().ThresholdStatus;
                var hmVolumeCheck = hmvThresholdStatus != HeatmapVolumeThresholdStatus.Low
                    && hmvThresholdStatus != HeatmapVolumeThresholdStatus.Normal;

                var hmvThresholdStatusForSecondLastPrice = hmVolumes[hmVolumes.Count - 2].ThresholdStatus;
                var hmVolumeCheckForSecondLastPrice = hmvThresholdStatusForSecondLastPrice != HeatmapVolumeThresholdStatus.Low
                    && hmvThresholdStatusForSecondLastPrice != HeatmapVolumeThresholdStatus.Normal;

                var wma9 = prices.GetWma(9);
                var wma21 = prices.GetWma(21);

                var volumeCheckForLong = wma9.Last().Wma > wma21.Last().Wma && (hmVolumeCheck || hmVolumeCheckForSecondLastPrice);
                var volumeCheckForShort = wma9.Last().Wma < wma21.Last().Wma && (hmVolumeCheck || hmVolumeCheckForSecondLastPrice);
                var isValidCandleForLong = price.IsGreenCandle && price.IsContentCandle;
                var isValidCandleForShort = price.IsRedCandle && price.IsContentCandle;

                var priceRangeBeforeSecondLastPrice = prices.GetRange(prices.Count - 1 - numberOfCandlesticksToLookBackBeforeCurrentPrice, numberOfCandlesticksToLookBackBeforeCurrentPrice);

                var levelPriceRangeBeforeSecondLastPriceTouched = levels
                    .Where(x =>
                    {
                        var center = (x.Key.Low + x.Key.High) / 2;
                        var centerOffset = center * (decimal)0.005;
                        var centerPoint = new NumericRange(center - centerOffset, center + centerOffset);
                        return secondLastPrice.CandleRange.Intersect(centerPoint);
                    })
                    .Where(x => !x.Key.Equals(price))
                    .ToList();

                Alert? alert = null;

                var atr = prices.GetAtr(14);

                if (levelPriceRangeBeforeSecondLastPriceTouched.Any())
                {
                    var levelLow = levelPriceRangeBeforeSecondLastPriceTouched.Select(x => x.Key.Low).Min();
                    var levelHigh = levelPriceRangeBeforeSecondLastPriceTouched.Select(x => x.Key.High).Max();
                    var center = (levelLow + levelHigh) / 2;
                    var centerPoint = new NumericRange(center, center);
                    var averageSwingPointIntersected = levelPriceRangeBeforeSecondLastPriceTouched.Select(x => x.Value.Count).Average();

                    var priceIntersectSecondLastPrice = price.CandleRange.Intersect(secondLastPrice.CandleRange);
                    var secondLastPriceIntersectCenterLevelPoint = secondLastPrice.CandleRange.Intersect(centerPoint);
                    var priceNotIntersectCenterLevelPoint = !price.CandleRange.Intersect(centerPoint);

                    var priceIntersectAnyLevelPoint = false;
                    var priceIntersectLevels = levels.Where(x => price.CandleRange.Intersect(x.Key.CandleRange));
                    decimal pricePointCenter = 0;
                    if (priceIntersectLevels.Any())
                    {
                        var pricePointInterectHigh = priceIntersectLevels.Select(x => x.Key.High).Max();
                        var pricePointInterectLow = priceIntersectLevels.Select(x => x.Key.Low).Min();
                        pricePointCenter = (pricePointInterectHigh + pricePointInterectLow) / 2;

                        var pricePointInterect = new NumericRange(pricePointCenter, pricePointCenter);
                        priceIntersectAnyLevelPoint = price.CandleRange.Intersect(pricePointInterect);
                    }

                    if (secondLastPrice.IsGreenCandle
                        && price.IsGreenCandle
                        && secondLastPriceIntersectCenterLevelPoint
                        && secondLastPrice.Close > centerPoint.High
                        && price.Close > secondLastPrice.Close
                        && price.Close > centerPoint.High
                        && priceIntersectSecondLastPrice
                        && priceNotIntersectCenterLevelPoint)
                    {
                        var message = priceIntersectAnyLevelPoint
                            ? $"Price {price.Close} ({price.Date:s}) > {centerPoint.High:F2} ({centerPoint.Low:F2} - {centerPoint.High:F2}), points: {averageSwingPointIntersected}, big body candle: {price.IsContentCandle}, *level touch*: {pricePointCenter:F2}"
                            : $"Price {price.Close} ({price.Date:s}) > {centerPoint.High:F2} ({centerPoint.Low:F2} - {centerPoint.High:F2}), points: {averageSwingPointIntersected}, big body candle: {price.IsContentCandle}";

                        if (hmVolumeCheck && isValidCandleForLong)
                        {
                            alert = new TopNBottomStrategyAlert
                            {
                                Ticker = ticker,
                                Message = "(O)" + message,
                                CreatedAt = DateTime.Now,
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
                                CreatedAt = DateTime.Now,
                                Strategy = "SwingPointsLiveTradingStrategy",
                                OrderPosition = OrderPosition.Long,
                                PositionAction = PositionAction.Open,
                                Timeframe = parameter.Timeframe
                            };
                        }
                        
                    }
                    else if (secondLastPrice.IsRedCandle
                        && price.IsRedCandle
                        && secondLastPriceIntersectCenterLevelPoint
                        && secondLastPrice.Close < centerPoint.Low
                        && price.Close < secondLastPrice.Close
                        && price.Close < centerPoint.Low
                        && priceIntersectSecondLastPrice
                        && priceNotIntersectCenterLevelPoint)
                    {
                        var message = priceIntersectAnyLevelPoint
                            ? $"Price {price.Close} ({price.Date:s}) < {centerPoint.Low:F2} ({centerPoint.Low:F2} - {centerPoint.High:F2}), points: {averageSwingPointIntersected}, big body candle: {price.IsContentCandle}, *level touch*: {pricePointCenter:F2}"
                            : $"Price {price.Close} ({price.Date:s}) < {centerPoint.Low:F2} ({centerPoint.Low:F2} - {centerPoint.High:F2}), points: {averageSwingPointIntersected}, big body candle: {price.IsContentCandle}";

                        if (hmVolumeCheck && isValidCandleForShort)
                        {
                            alert = new TopNBottomStrategyAlert
                            {
                                Ticker = ticker,
                                Message = "(O)" + message,
                                CreatedAt = DateTime.Now,
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
                                CreatedAt = DateTime.Now,
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
                    && priceNotIntersectAnyTops)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking above down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})",
                        CreatedAt = DateTime.Now,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderPosition = OrderPosition.Long,
                        PositionAction = PositionAction.Open,
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
                        && priceGreaterThanProjectedPrice)
                    {
                        alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} ({price.Date:s}) is rebounding on down trend line {lineStart.High} ({lineEnds.Item1.Date:s}) - {lineEnd.High} ({lineEnds.Item2.Date:s})",
                            CreatedAt = DateTime.Now,
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
                    && priceNotIntersectAnyBottoms)
                {
                    alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                        CreatedAt = DateTime.Now,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderPosition = OrderPosition.Short,
                        PositionAction = PositionAction.Open,
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
                        && priceLowerThanProjectedPrice)
                    {
                        alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} ({price.Date:s}) is touching from below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                            CreatedAt = DateTime.Now,
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

        protected virtual void OnAlertCreated(AlertEventArgs e)
        {
            AlertCreated?.Invoke(this, e);
        }
    }
}
