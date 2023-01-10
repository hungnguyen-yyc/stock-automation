using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class LinearTrendlineDetector
    {
        public static int FindTrendline(IList<(DateTimeOffset, decimal)> points, out DateTimeOffset start, out DateTimeOffset end)
        {
            // Initialize start and end points
            start = points[0].Item1;
            end = points[points.Count - 1].Item1;
            int maxIntersections = 0;

            // Loop through all possible start and end points
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    // Calculate the time difference between the start and end points
                    TimeSpan timeDifference = points[j].Item1 - points[i].Item1;

                    // Calculate the slope of the trendline
                    decimal slope = (points[j].Item2 - points[i].Item2) / (decimal)timeDifference.TotalDays;

                    // Initialize the count of intersections
                    int intersections = 0;

                    // Loop through all data points to check for intersections
                    for (int k = 0; k < points.Count; k++)
                    {
                        // Calculate the time difference between the start point and the current x-value
                        TimeSpan xDifference = points[k].Item1 - points[i].Item1;

                        // Calculate the y-value of the trendline at the current x-value
                        decimal y = points[i].Item2 + slope * (decimal)xDifference.TotalDays;

                        // Check if the trendline intersects the data point
                        if (Math.Abs(y - points[k].Item2) < 0.01m)
                        {
                            intersections++;
                        }
                    }

                    // Update the start and end points if the trendline intersects more data points
                    if (intersections > maxIntersections)
                    {
                        start = points[i].Item1;
                        end = points[j].Item1;
                        maxIntersections = intersections;
                    }
                }
            }

            // Return the number of intersections
            return maxIntersections;
        }
    }
}
