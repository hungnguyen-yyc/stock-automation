using Skender.Stock.Indicators;
using Stock.Shared.Models;

namespace Stock.Strategies;

public class MfiDivergenceDetector
{
    private const int OverboughtThreshold = 78;
    private const int OversoldThreshold = 22;
    private const int NeutralZoneTop = 60;
    private const int NeutralZoneBottom = 40;
    private const int TotalNumberOfCandlesToCheckForDivergence = 10;
    private const int NumberOfCandlesToCheckForDivergence = 8; // Actual number of candles to check for divergence, the rest is for checking reversal
    
    public static bool CheckBullishDivergenceOnRunningCandles(IReadOnlyList<Price> prices, int mfiLookBackPeriod)
    {
        var mfiResults = prices.GetMfi(mfiLookBackPeriod).Skip(prices.Count - TotalNumberOfCandlesToCheckForDivergence).ToList();
        var priceResults = prices.Skip(prices.Count - TotalNumberOfCandlesToCheckForDivergence).ToList();
        
        // 1. Check first NumberOfCandlesToCheckForDivergence MFI values for trending up
        var mfiUps = mfiResults
            .Select(x => x.Mfi)
            .Skip(1) // Skip first one to compare with the next one
            .Where((num, index) => num > mfiResults[index].Mfi) // Check if the next one is greater than the previous one
            .Count();
        var moreUpsThanDowns = mfiUps * 100 / TotalNumberOfCandlesToCheckForDivergence >= 60;
        var lastMfiIsGreaterThanFirst = mfiResults.Last().Mfi > mfiResults.First().Mfi;
        var anyOfFirst3MfiIsInOversoldZone = mfiResults.Take(3).Any(x => x.Mfi <= OversoldThreshold);
        var mfiCheck = moreUpsThanDowns && lastMfiIsGreaterThanFirst && anyOfFirst3MfiIsInOversoldZone;
        
        // 2. Check first NumberOfCandlesToCheckForDivergence prices for trending down
        var priceDowns = priceResults
            .Select(x => x.Close)
            .Skip(1) // Skip first one to compare with the next one
            .Where((num, index) => num < priceResults[index].Close) // Check if the next one is less than the previous one
            .Count();
        var moreDownsThanUps = priceDowns * 100 / NumberOfCandlesToCheckForDivergence >= 60;
        var lastPriceIsLessThanFirst = priceResults.Last().Close < priceResults.First().Close;
        var priceCheck = moreDownsThanUps && lastPriceIsLessThanFirst;
        
        return mfiCheck && priceCheck;
    }

    public static bool CheckBearishDivergenceOnRunningCandles(IReadOnlyList<Price> prices, int mfiLookBackPeriod)
    {
        var mfiResults = prices.GetMfi(mfiLookBackPeriod).Skip(prices.Count - TotalNumberOfCandlesToCheckForDivergence).ToList();
        var priceResults = prices.Skip(prices.Count - TotalNumberOfCandlesToCheckForDivergence).ToList();
        
        // 1. Check first NumberOfCandlesToCheckForDivergence MFI values for trending down
        var mfiDowns = mfiResults
            .Select(x => x.Mfi)
            .Skip(1) // Skip first one to compare with the next one
            .Where((num, index) => num < mfiResults[index].Mfi) // Check if the next one is less than the previous one
            .Count();
        var moreDownsThanUps = mfiDowns * 100 / NumberOfCandlesToCheckForDivergence >= 60;
        var lastMfiIsLessThanFirst = mfiResults.Last().Mfi < mfiResults.First().Mfi;
        var anyOfFirst3MfiIsInOverboughtZone = mfiResults.Take(3).Any(x => x.Mfi >= OverboughtThreshold);
        var mfiCheck = moreDownsThanUps && lastMfiIsLessThanFirst && anyOfFirst3MfiIsInOverboughtZone;
        
        // 2. Check first NumberOfCandlesToCheckForDivergence prices for trending up
        var priceUps = priceResults
            .Select(x => x.Close)
            .Skip(1) // Skip first one to compare with the next one
            .Where((num, index) => num > priceResults[index].Close) // Check if the next one is greater than the previous one
            .Count();
        var moreUpsThanDowns = priceUps * 100 / NumberOfCandlesToCheckForDivergence >= 60;
        var lastPriceIsGreaterThanFirst = priceResults.Last().Close > priceResults.First().Close;
        var priceCheck = moreUpsThanDowns && lastPriceIsGreaterThanFirst;
        
        return mfiCheck && priceCheck;
    }
}