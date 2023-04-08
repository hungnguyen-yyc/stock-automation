namespace StockSignalScanner.Indicators
{
    using System;
    using System.Collections.Generic;

    public class TrendChecker
    {
        public enum Trend
        {
            Flat,
            Increasing,
            Decreasing,
            Fluctuating,
            FluctuatingThenIncreasing,
            FluctuatingThenDecreasing
        }

        public static Trend CheckTrend(List<double> numbers)
        {
            if (numbers == null || numbers.Count < 2)
            {
                // Handle cases where the list is null or has less than 2 elements
                throw new ArgumentException("The input list must have at least 2 elements.");
            }

            bool increasing = false;
            bool decreasing = false;
            bool fluctuating = false;
            bool fluctuatingThenIncreasing = false;
            bool fluctuatingThenDecreasing = false;

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] > numbers[i - 1])
                {
                    // If any number is greater than the previous number,
                    // set increasing flag to true
                    increasing = true;

                    if (fluctuating)
                    {
                        // If the list was previously fluctuating and now increasing,
                        // set fluctuatingThenIncreasing flag to true
                        fluctuatingThenIncreasing = numbers[0] < numbers[numbers.Count - 1];
                    }
                }
                else if (numbers[i] < numbers[i - 1])
                {
                    // If any number is less than the previous number,
                    // set decreasing flag to true
                    decreasing = true;

                    if (fluctuating)
                    {
                        // If the list was previously fluctuating and now decreasing,
                        // set fluctuatingThenDecreasing flag to true
                        fluctuatingThenDecreasing = numbers[0] > numbers[numbers.Count - 1];
                    }
                }

                if (increasing && decreasing)
                {
                    // If both increasing and decreasing flags are set,
                    // the list is mixed
                    fluctuating = true;
                }
            }


            if (fluctuatingThenIncreasing)
            {
                return Trend.FluctuatingThenIncreasing;
            }
            else if (fluctuatingThenDecreasing)
            {
                return Trend.FluctuatingThenDecreasing;
            }
            else if (increasing)
            {
                return Trend.Increasing;
            }
            else if (decreasing)
            {
                return Trend.Decreasing;
            }
            else if (fluctuating)
            {
                return Trend.Fluctuating;
            }
            else
            {
                // If neither increasing, decreasing, fluctuating, fluctuatingThenIncreasing,
                // nor fluctuatingThenDecreasing flags are set, the list is fluctuating
                return Trend.Flat;
            }
        }
    }

}
