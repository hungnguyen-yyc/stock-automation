using Stock.Shared.Models;
using Stock.Shared.Extensions;
using Stock.Strategies.Trend;
using Stock.Strategies.Parameters;

namespace Stock.Strategies.Helpers
{
    internal class SwingPointAnalyzer
    {
        public static IReadOnlyDictionary<Price, HashSet<Price>> GetLevels(List<Price> prices, int numberOfCandlesToLookBack)
        {
            var tops = GetNTops(prices, numberOfCandlesToLookBack);
            var bottoms = GetNBottoms(prices, numberOfCandlesToLookBack);
            var combined = new Dictionary<Price, HashSet<Price>>();

            var flattenedTops = tops.SelectMany(x => x.Value).Concat(tops.Keys).ToList();
            var flattenedBottoms = bottoms.SelectMany(x => x.Value).Concat(bottoms.Keys).ToList();
            var flattened = flattenedTops.Concat(flattenedBottoms).ToList();

            for ( var i = 0; i < flattened.Count; i++)
            {
                for (var j = i + 1; j < flattened.Count; j++)
                {
                    var current = flattened[i];
                    var next = flattened[j];

                    if (current.CandleRange.Intersect(next.CandleRange))
                    {
                        var hasKey = combined.Keys.Any(x => x.CandleRange.Intersect(current.CandleRange));

                        if (hasKey)
                        {
                            var key = combined.Keys.First(x => x.CandleRange.Intersect(current.CandleRange));
                            combined[key].Add(next);
                        }
                        else
                        {
                            combined.Add(current, new HashSet<Price>() { next });
                        }
                    }
                }
            }
            
            combined = combined.OrderBy(x => x.Key.Date).ToDictionary(x => x.Key, x => x.Value);

            return combined;
        }

        /**
            * returns a map of first swing low as key and the next n swing lows that intersect with the key one as value,
            * it can be double bottom or triple bottom or more, but we don't check for 2 or 3 consecutive swing lows
            */
        public static IReadOnlyDictionary<Price, HashSet<Price>> GetNBottoms(List<Price> prices, int numberOfCandlesToLookBack)
        {
            var swingLows = FindSwingLows(prices, numberOfCandlesToLookBack);
            var result = new Dictionary<Price, HashSet<Price>>();

            for (var i = 0; i < swingLows.Count; i++)
            {
                for (var j = i + 1; j < swingLows.Count; j++)
                {
                    var currentSwingLow = swingLows[i];
                    var nextSwingLow = swingLows[j];

                    if (currentSwingLow.BottomHalfOfCandle.Intersect(nextSwingLow.BottomHalfOfCandle))
                    {
                        var hasKey = result.Keys.Any(x => x.BottomHalfOfCandle.Intersect(currentSwingLow.BottomHalfOfCandle));

                        if (hasKey)
                        {
                            var key = result.Keys.First(x => x.BottomHalfOfCandle.Intersect(currentSwingLow.BottomHalfOfCandle));
                            result[key].Add(nextSwingLow);
                        }
                        else
                        {
                            result.Add(currentSwingLow, new HashSet<Price>() { nextSwingLow });
                        }
                    }
                }
            }

            return result;
        }

        /**
         * returns a map of first swing high as key and the next n swing highs that intersect with the key one as value,
         * it can be double top or triple top or more
         * this means any swing highs or lows that has no intersection with the next n swing highs or lows are not considered
         * thus, missing ATH or ATL
         **/
        public static IReadOnlyDictionary<Price, HashSet<Price>> GetNTops(List<Price> prices, int numberOfCandlesToLookBack)
        {
            var swingHighs = FindSwingHighs(prices, numberOfCandlesToLookBack);
            var result = new Dictionary<Price, HashSet<Price>>();

            for (var i = 0; i < swingHighs.Count; i++)
            {
                for (var j = i + 1; j < swingHighs.Count; j++)
                {
                    var currentSwingHigh = swingHighs[i];
                    var nextSwingHigh = swingHighs[j];

                    if (currentSwingHigh.TopHalfOfCandle.Intersect(nextSwingHigh.TopHalfOfCandle))
                    {
                        var hasKey = result.Keys.Any(x => x.TopHalfOfCandle.Intersect(currentSwingHigh.TopHalfOfCandle));

                        if (hasKey)
                        {
                            var key = result.Keys.First(x => x.TopHalfOfCandle.Intersect(currentSwingHigh.TopHalfOfCandle));
                            result[key].Add(nextSwingHigh);
                        }
                        else
                        {
                            result.Add(currentSwingHigh, new HashSet<Price>() { nextSwingHigh });
                        }
                    }
                }
            }

            return result;
        }

        /**
         * TODO: fix this method, it's not working properly
         * 
         * check the last n consecutive candles to see if they are forming a descending channel
         * number of candles to check is the number of candles to look back to check for the channel
         * number of candles to look back is the number of candles to look back to find the swing highs and lows, we want to keep this number small because we're checking for consecutive candles
         */
        public static Channel? CheckRunningCandlesFormingChannel(List<Price> prices, int numberOfCandlesToCheck = 10, int numberOfCandlesToLookBack = 3)
        {
            var priceToCheck = prices.Skip(prices.Count - numberOfCandlesToCheck).Take(numberOfCandlesToCheck).ToList();
            var swingLows = priceToCheck;
            var swingHighs = priceToCheck;

            if (prices.Count < numberOfCandlesToCheck || swingLows.Count < 2 || swingHighs.Count < 2)
                return null;

            var swingHighLines = swingHighs.Select(x => priceToCheck.GetPriceHighLineCoordinate(x)).ToList();
            var swingLowLines = swingLows.Select(x => priceToCheck.GetPriceLowLineCoordinate(x)).ToList();

            // check for channel with swing highs
            var i = 0;
            var j = swingHighs.Count - 1;
            var count = 0;
            var lineAndCount = new Dictionary<Tuple<Price, Price>, int>();
            while (i < j)
            {
                var currentSwingHigh = swingHighs[i];
                var lastSwingHigh = swingHighs[j];
                var currentPointIndex = priceToCheck.IndexOf(currentSwingHigh);
                var runningPointIndex = priceToCheck.IndexOf(lastSwingHigh);
                var lineFromCurrentToLastPoint = new Line(new Point(currentPointIndex, currentSwingHigh.High), new Point(runningPointIndex, lastSwingHigh.High));
                for (var k = i; k < j; k++)
                {
                    var currentPoint = swingHighs[k];
                    var currentPointHighLine = priceToCheck.GetPriceHighLineCoordinate(currentPoint);
                    var currentPointLowLine = priceToCheck.GetPriceBodyStartToLowLineCoordinate(currentPoint);
                    if (DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, currentPointHighLine.Start, currentPointHighLine.End))
                    {
                        count++;
                    }
                    else if (DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, currentPointLowLine.Start, currentPointLowLine.End))
                    {
                        count = 0;
                        break;
                    }
                }

                if (count > 0)
                {
                    lineAndCount.Add(new Tuple<Price, Price>(currentSwingHigh, lastSwingHigh), count);
                }
                i++;
                j--;
            }

            var highLineWithMostCrosses = lineAndCount.OrderByDescending(x => x.Value).FirstOrDefault().Key;

            // check for channel with swing lows
            i = 0;
            j = swingLows.Count - 1;
            count = 0;
            lineAndCount = new Dictionary<Tuple<Price, Price>, int>();
            while (i < j)
            {
                var currentSwingLow = swingLows[i];
                var lastSwingLow = swingLows[j];
                var currentPointIndex = priceToCheck.IndexOf(currentSwingLow);
                var runningPointIndex = priceToCheck.IndexOf(lastSwingLow);
                var lineFromCurrentToLastPoint = new Line(new Point(currentPointIndex, currentSwingLow.Low), new Point(runningPointIndex, lastSwingLow.Low));
                for (var k = i; k < j; k++)
                {
                    var currentPoint = swingLows[k];
                    var currentPointHighLine = priceToCheck.GetPriceBodyStartToHighLineCoordinate(currentPoint);
                    var currentPointLowLine = priceToCheck.GetPriceLowLineCoordinate(currentPoint);
                    if (DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, currentPointLowLine.Start, currentPointLowLine.End))
                    {
                        count++;
                    }
                    else if (DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, currentPointHighLine.Start, currentPointHighLine.End))
                    {
                        count = 0;
                        break;
                    }
                }

                if (count > 0)
                {
                    lineAndCount.Add(new Tuple<Price, Price>(currentSwingLow, lastSwingLow), count);
                }
                i++;
                j--;
            }

            var lowLineWithMostCrosses = lineAndCount.OrderByDescending(x => x.Value).FirstOrDefault().Key;

            if (highLineWithMostCrosses == null || lowLineWithMostCrosses == null)
                return null;

            return new Channel(priceToCheck, highLineWithMostCrosses, lowLineWithMostCrosses);
        }

        /**
         * We need to pass in prices because we need to know the index of the price/swing point in order to draw the line
         * We then use prices to find swing points and then convert those swing points to coordinates
         * We loop through the swing points and draw lines between them, we draw from the top of first swing point to the top of the next n swing points
         * we want to make sure that lines don't cross body part of the candle
         * then we check for lines that has more than 3 crosses
         */
        public static Tuple<Price, Price>[] GetTrendlines(List<Price> prices, SwingPointStrategyParameter param, bool isSwingHigh = true)
        {
            var numberOfCandlesToLookBack = param.NumberOfCandlesticksToLookBack;
            var numberOfTouchesToDrawTrendLine = param.NumberOfTouchesToDrawTrendLine;
            var numberOfSwingPointsToLookBack = param.NumberOfSwingPointsToLookBack;
            var pointLines = new List<Tuple<Price, Price>>();

            var swingPoints = isSwingHigh ? 
                FindSwingHighs(prices, numberOfCandlesToLookBack) :
                FindSwingLows(prices, numberOfCandlesToLookBack);

            var lineCrossCount = new Dictionary<Line, int>();
            if (swingPoints.Count < 3)
                return pointLines.ToArray();

            var swingPointLines = isSwingHigh ?
                swingPoints.Select(x => prices.GetPriceHighLineCoordinate(x)).ToList()! :
                swingPoints.Select(x => prices.GetPriceLowLineCoordinate(x)).ToList()!;

            for (var i = 0; i < swingPoints.Count - 1; i++)
            {
                for (var j = i + 1; j < swingPoints.Count; j++)
                {
                    if (j - i > numberOfSwingPointsToLookBack)
                        continue;

                    var currentPoint = swingPoints[i];
                    var runningPoint = swingPoints[j];
                    var currentPointIndex = prices.IndexOf(currentPoint);
                    var runningPointIndex = prices.IndexOf(runningPoint);

                    var lineFromCurrentToLastPoint = isSwingHigh ?
                        new Line(new Point(currentPointIndex, currentPoint.High), new Point(runningPointIndex, runningPoint.High)) :
                        new Line(new Point(currentPointIndex, currentPoint.Low), new Point(runningPointIndex, runningPoint.Low));
                    
                    var count = 0;
                    foreach (var line in swingPointLines)
                    {
                        if (DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, line.Start, line.End))
                        {
                            var priceStartIndex = prices.IndexOf(currentPoint);
                            var priceEndIndex = prices.IndexOf(runningPoint);
                            var priceRange = prices.Skip(priceStartIndex).Take(priceEndIndex - priceStartIndex).ToList();
                            var bodyRange = isSwingHigh ? 
                                priceRange.Select(x => prices.GetPriceBodyStartToLowLineCoordinate(x)).ToList() : 
                                priceRange.Select(x => prices.GetPriceBodyStartToHighLineCoordinate(x)).ToList();
                            var highOrLowRange = isSwingHigh ? 
                                priceRange.Select(x => prices.GetPriceHighLineCoordinate(x)).ToList() : 
                                priceRange.Select(x => prices.GetPriceLowLineCoordinate(x)).ToList();

                            var mainLineNotCrossBodyRange = bodyRange.All(x => !DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, x.Start, x.End));
                            var crossHighOrLowCount = isSwingHigh ? 
                                highOrLowRange.Count(x => DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, x.Start, x.End)) :
                                highOrLowRange.Count(x => DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, x.Start, x.End));

                            if (mainLineNotCrossBodyRange)
                            {
                                count += crossHighOrLowCount;
                            }
                            else
                            {
                                count = 0;
                                continue;
                            }
                        }
                    }

                    if (count > 0)
                        lineCrossCount.Add(lineFromCurrentToLastPoint, count);
                }
            }

            foreach (var lineCount in lineCrossCount)
            {
                if (lineCount.Value < numberOfTouchesToDrawTrendLine)
                    continue;

                var line = lineCount.Key;
                var lineStart = line.Start;
                var lineEnd = line.End;

                var highAtStart = prices[(int)lineStart.X];
                var highAtEnd = prices[(int)lineEnd.X];

                pointLines.Add(new Tuple<Price, Price>(highAtStart, highAtEnd));
            }

            return pointLines.ToArray();
        }


        public static bool DoLinesIntersect(Point A, Point B, Point C, Point D)
        {
            // Check if the orientations of the points are different
            bool orientationsABCD = GetOrientation(A, B, C) != GetOrientation(A, B, D);
            bool orientationsCDA = GetOrientation(C, D, A) != GetOrientation(C, D, B);

            // If orientations are different, it indicates intersection
            if (orientationsABCD && orientationsCDA)
                return true;

            // Special case: endpoints of one line segment lie on the other line segment
            if (GetOrientation(A, B, C) == 0 && PointOnSegment(A, C, B))
                return true;
            if (GetOrientation(A, B, D) == 0 && PointOnSegment(A, D, B))
                return true;
            if (GetOrientation(C, D, A) == 0 && PointOnSegment(C, A, D))
                return true;
            if (GetOrientation(C, D, B) == 0 && PointOnSegment(C, B, D))
                return true;

            return false;
        }

        // Helper method to check if a point lies on a line segment
        private static bool PointOnSegment(Point p, Point q, Point r)
        {
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }

        /**
         * To check if two lines cross, you typically need the endpoints of both lines.
         * Each line is defined by two points, and the intersection of these lines occurs if the line segments defined by the points intersect.
         * For example, if you have Line 1 defined by points A and B, and Line 2 defined by points C and D, you can check for intersection by determining whether the line segment AB intersects with CD.
         * The GetOrientation function you mentioned earlier might be part of an algorithm used to check for intersection.
         * By comparing the orientations of the four points (A, B, C, D), you can infer whether the line segments AB and CD intersect.
         * Here's a simplified overview:
         * If the orientations of A, B, and C are all different, or the orientations of A, B, and D are all different, then the line segments intersect.
         * If the orientations are the same, the line segments do not intersect.
         * This method doesn't directly use the term "cross," but it checks for the intersection of line segments defined by four points.
         **/
        private static int GetOrientation(Point p1, Point p2, Point p3)
        {
            var val = (p2.Y - p1.Y) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Y - p2.Y);

            if (val == 0) return 0;  // Collinear
            return (val > 0) ? 1 : 2; // Clockwise or counterclockwise
        }

        public static List<Price> FindSwingLows(List<Price> prices, int numberOfCandlesToLookBack)
        {
            return FindSwingPoints(prices, numberOfCandlesToLookBack, (price, currentPrice) => price.Low < currentPrice.Low);
        }

        public static List<Price> FindSwingHighs(List<Price> prices, int numberOfCandlesToLookBack)
        {
            return FindSwingPoints(prices, numberOfCandlesToLookBack, (price, currentPrice) => price.High > currentPrice.High);
        }

        private static List<Price> FindSwingPoints(List<Price> prices, int numberOfCandlesToLookBack, Func<Price, Price, bool> compare)
        {
            List<Price> swingPoints = new List<Price>();

            for (int i = numberOfCandlesToLookBack; i < prices.Count; i++)
            {
                var currentPrice = prices[i];

                bool isSwingPoint = true;
                var innerRange = i < prices.Count - numberOfCandlesToLookBack ? i + numberOfCandlesToLookBack : prices.Count - 1;

                for (int j = i - numberOfCandlesToLookBack; j <= innerRange; j++)
                {
                    if (j == i)
                        continue;

                    var price = prices[j];

                    // < instead of <= because we want to allow for equal lows/highs
                    // having <= would mean that the current price is not a swing low/high if it is equal to a previous low/high
                    // meaning we would miss out on a swing low/high
                    if (compare(price, currentPrice))
                    {
                        isSwingPoint = false;
                        break;
                    }
                }

                if (isSwingPoint)
                {
                    swingPoints.Add(currentPrice);
                }
            }

            return swingPoints;
        }
    }
}
