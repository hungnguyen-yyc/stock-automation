using StockSignalScanner.Models;

internal static class MyProcessHelpers
{
    internal static string API_KEY = "bc00404c44fcc9fe338ac768f222f6ab";
    internal static string PATH = @"C:\Users\hnguyen\Documents\stock-scan-logs-day-trade";
    internal static int TIME_BETWEEN_PIVOTS = 45;
    internal static int NUMBER_OF_5_MIN_CANDLES_IN_A_DAY = 79;
    internal static int TOP_OR_BOTTOM_LIMIT = 30;
    internal static int NUMBER_OF_SWING_POINTS = 5;

    public static List<Price> FindSwingLows(List<Price> prices, int range)
    {
        List<Price> swingLows = new List<Price>();

        for (int i = range; i < prices.Count; i++)
        {
            var currentPrice = prices[i];

            bool isSwingLow = true;
            var innerRange = i < prices.Count - range ? i + range : prices.Count - 1;

            for (int j = i - range; j <= innerRange; j++)
            {
                if (j == i)
                    continue;

                var price = prices[j];

                if (price.Low <= currentPrice.Low)
                {
                    isSwingLow = false;
                    break;
                }
            }

            if (isSwingLow)
            {
                swingLows.Add(currentPrice);
            }
        }

        return swingLows;
    }

    public static List<Price> FindSwingHighs(List<Price> prices, int range)
    {
        var swingHighs = new List<Price>();

        for (int i = range; i < prices.Count; i++)
        {
            var currentPrice = prices[i];

            bool isSwingHigh = true;
            var innerRange = i < prices.Count - range ? i + range : prices.Count - 1;

            for (int j = i - range; j <= innerRange; j++)
            {
                if (j == i)
                    continue;

                var price = prices[j];

                if (price.High >= currentPrice.High)
                {
                    isSwingHigh = false;
                    break;
                }
            }

            if (isSwingHigh)
            {
                swingHighs.Add(currentPrice);
            }
        }

        return swingHighs;
    }

    public enum TrendType
    {
        Uptrend,
        Downtrend,
        Reversal,
        Unknown
    }

    public static TrendType DetermineTrend(List<Price> swingHighs, List<Price> swingLows)
    {
        if (swingHighs.Count == 0 || swingLows.Count == 0)
        {
            return TrendType.Unknown;
        }

        // Determine the last swing high and swing low
        IPrice lastSwingHigh = swingHighs[swingHighs.Count - 1];
        IPrice lastSwingLow = swingLows[swingLows.Count - 1];

        // Check if the last swing high is more recent than the last swing low
        if (lastSwingHigh.Date > lastSwingLow.Date)
        {
            return TrendType.Uptrend;
        }
        else if (lastSwingHigh.Date < lastSwingLow.Date)
        {
            return TrendType.Downtrend;
        }

        // If the last swing high and swing low have the same date, further analysis is needed

        // Check if the last swing high is higher than the last swing low
        if (lastSwingHigh.High > lastSwingLow.Low)
        {
            return TrendType.Uptrend;
        }
        else if (lastSwingHigh.High < lastSwingLow.Low)
        {
            return TrendType.Downtrend;
        }

        // If the last swing high and swing low have the same date and same price level, further analysis is needed

        // Check if the previous swing high is more recent than the previous swing low
        if (swingHighs.Count > 1 && swingLows.Count > 1)
        {
            IPrice prevSwingHigh = swingHighs[swingHighs.Count - 2];
            IPrice prevSwingLow = swingLows[swingLows.Count - 2];

            if (prevSwingHigh.Date > prevSwingLow.Date)
            {
                return TrendType.Uptrend;
            }
            else if (prevSwingHigh.Date < prevSwingLow.Date)
            {
                return TrendType.Downtrend;
            }
        }

        // If no clear trend is identified, it can be considered a reversal
        return TrendType.Reversal;
    }
}