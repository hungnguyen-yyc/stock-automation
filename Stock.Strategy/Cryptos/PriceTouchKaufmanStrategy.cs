using Skender.Stock.Indicators;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies.Cryptos;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;

namespace Stock.Strategies;

public class PriceTouchKaufmanStrategy : ICryptoStrategy
{
    public string Description => "To check when Price touched HMAs after cross, will it continue to go up or down.";
    public event AlertEventHandler? EntryAlertCreated;
    public event AlertEventHandler? ExitAlertCreated;

    public void CheckForBullishEntry(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var price = prices.Last();
        var secondLastPrice = prices.ElementAt(prices.Count - 2);
        var thirdLastPrice = prices.ElementAt(prices.Count - 3);
        
        var kama14s = prices.GetKama(14, 5, 30).Select(x => x.Kama ?? 0).ToList();
        var lastKama14 = kama14s.Last();
        var secondLastKama14 = kama14s.ElementAt(kama14s.Count - 2);
        var thirdLastKama14 = kama14s.ElementAt(kama14s.Count - 3);
        
        var thirdLastPriceTouchedThirdLastKama = thirdLastPrice.CandleRange.Intersect(new NumericRange((decimal)thirdLastKama14, (decimal)thirdLastKama14));
        var secondLastPriceNotTouchedThirdLastKama = !secondLastPrice.CandleRange.Intersect(new NumericRange((decimal)thirdLastKama14, (decimal)thirdLastKama14));
        var priceNotTouchedKama = !price.CandleRange.Intersect(new NumericRange((decimal)secondLastKama14, (decimal)secondLastKama14));
        var priceGreaterThanKama = price.Close > (decimal)lastKama14;
        
        if (thirdLastPriceTouchedThirdLastKama 
            && secondLastPriceNotTouchedThirdLastKama 
            && priceNotTouchedKama
            && priceGreaterThanKama)
        {
            var ticker = CryptosToTrade.CryptoEnumToName[crypto];
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{ticker} Enter position at {price.Close} at {price.Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = price.Date,
                Strategy = nameof(PriceTouchHmaAfterCrossStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe,
                PriceClosed = price.Close
            };
            
            EntryAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }

    public void CheckForBullishExit(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var price = prices.Last();
        var secondLastPrice = prices.ElementAt(prices.Count - 2);
        var thirdLastPrice = prices.ElementAt(prices.Count - 3);
        var fourthLastPrice = prices.ElementAt(prices.Count - 4);
        var fifthLastPrice = prices.ElementAt(prices.Count - 5);
        
        var kama14s = prices.GetKama(14, 5, 30).Select(x => x.Kama ?? 0).ToList();
        var kama10s = prices.GetKama(10, 2, 30).Select(x => x.Kama ?? 0).ToList();
        var lastKama10 = kama10s.Last();
        var secondLastKama10 = kama10s.ElementAt(kama10s.Count - 2);
        var thirdLastKama10 = kama10s.ElementAt(kama10s.Count - 3);
        var fourthLastKama10 = kama10s.ElementAt(kama10s.Count - 4);
        var fifthLastKama10 = kama10s.ElementAt(kama10s.Count - 5);
        
        var lastPriceNotTouchedLastKama = !price.CandleRange.Intersect(new NumericRange((decimal)lastKama10, (decimal)lastKama10)) && price.Close < (decimal)lastKama10;
        var secondLastPriceTouchedSecondLastKama = secondLastPrice.CandleRange.Intersect(new NumericRange((decimal)secondLastKama10, (decimal)secondLastKama10));
        var thirdLastPriceNotTouchedThirdLastKama = !thirdLastPrice.CandleRange.Intersect(new NumericRange((decimal)thirdLastKama10, (decimal)thirdLastKama10)) || thirdLastPrice.Close > (decimal)thirdLastKama10;
        var fourthLastPriceNotTouchedFourthLastKama = !fourthLastPrice.CandleRange.Intersect(new NumericRange((decimal)fourthLastKama10, (decimal)fourthLastKama10)) || fourthLastPrice.Close > (decimal)fourthLastKama10;
        var fifthLastPriceNotTouchedFifthLastKama = !fifthLastPrice.CandleRange.Intersect(new NumericRange((decimal)fifthLastKama10, (decimal)fifthLastKama10)) || fifthLastPrice.Close > (decimal)fifthLastKama10;
        var lastPriceCrossedBelowLastKama = lastPriceNotTouchedLastKama
                                            && secondLastPriceTouchedSecondLastKama
                                            && thirdLastPriceNotTouchedThirdLastKama
                                            && fourthLastPriceNotTouchedFourthLastKama
                                            && fifthLastPriceNotTouchedFifthLastKama;
        
        // this is because when we open position, we are looking at the last Kama value to always above Kama14, so we need to check if the price is below the last Kama value
        var priceCloseBelowKama14 = price.Close < (decimal)kama14s.Last();
        
        if (lastPriceCrossedBelowLastKama || priceCloseBelowKama14)
        {
            var ticker = CryptosToTrade.CryptoEnumToName[crypto];
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{ticker} Exit position at {price.Close} at {price.Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = price.Date,
                Strategy = nameof(PriceTouchHmaAfterCrossStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Close,
                Timeframe = parameter.Timeframe,
                PriceClosed = price.Close
            };
            ExitAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}