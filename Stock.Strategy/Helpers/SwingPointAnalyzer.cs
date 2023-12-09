using Stock.Shared.Models;
using Stock.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies.Helpers
{
    internal class SwingPointAnalyzer
    {
        /**
         * We need to pass in prices because we need to know the index of the price/swing point in order to draw the line
         * We then use prices to find swing points and then convert those swing points to coordinates
         * We loop through the swing points and draw lines between them, we draw from the top of first swing point to the top of the next n swing points
         * we want to make sure that lines don't cross body part of the candle
         * then we check for lines that has more than 3 crosses
         */
        public static Tuple<Price, Price>[] CheckSwingPointsOnSameLines(List<Price> prices, int numberOfCandlesToLookBack, bool isSwingHigh = true)
        {
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

            for (var i = 0; i < swingPoints.Count; i++)
            {
                for (var j = i + 2; j < swingPoints.Count - 3; j++)
                {
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

                            var mainLineNotCrossBodyRange = bodyRange.All(x => !DoLinesIntersect(lineFromCurrentToLastPoint.Start, lineFromCurrentToLastPoint.End, x.Start, x.End));

                            if (mainLineNotCrossBodyRange && Math.Abs(i - j) <= 6)
                                count++;
                        }
                    }

                    if (count > 0)
                        lineCrossCount.Add(lineFromCurrentToLastPoint, count);
                }
            }

            foreach (var lineCount in lineCrossCount)
            {
                if (lineCount.Value < 3)
                    continue;

                var line = lineCount.Key;
                var lineStart = line.Start;
                var lineEnd = line.End;

                // as check for break out so we want to check for lower highs or higher lows
                var lineDirectionToIgnore = isSwingHigh ? lineStart.Y < lineEnd.Y : lineStart.Y > lineEnd.Y;
                if (lineDirectionToIgnore)
                    continue;

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
