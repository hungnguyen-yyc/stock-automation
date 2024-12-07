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
                Message = $"{ticker} close ({price.Close}) above Kama14 value {lastKama14} at price's date {price.Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = price.Date,
                Strategy = nameof(PriceTouchKaufmanStrategy),
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
        
        var lastPriceNotTouchedLastKama10 = !price.CandleRange.Intersect(new NumericRange((decimal)lastKama10, (decimal)lastKama10)) && price.Close < (decimal)lastKama10;
        var secondLastPriceTouchedSecondLastKama10 = secondLastPrice.CandleRange.Intersect(new NumericRange((decimal)secondLastKama10, (decimal)secondLastKama10));
        var thirdLastPriceNotTouchedThirdLastKama10 = !thirdLastPrice.CandleRange.Intersect(new NumericRange((decimal)thirdLastKama10, (decimal)thirdLastKama10)) || thirdLastPrice.Close > (decimal)thirdLastKama10;
        var fourthLastPriceNotTouchedFourthLastKama10 = !fourthLastPrice.CandleRange.Intersect(new NumericRange((decimal)fourthLastKama10, (decimal)fourthLastKama10)) || fourthLastPrice.Close > (decimal)fourthLastKama10;
        var fifthLastPriceNotTouchedFifthLastKama10 = !fifthLastPrice.CandleRange.Intersect(new NumericRange((decimal)fifthLastKama10, (decimal)fifthLastKama10)) || fifthLastPrice.Close > (decimal)fifthLastKama10;
        var lastPriceCrossedBelowLastKama10 = lastPriceNotTouchedLastKama10
                                            && secondLastPriceTouchedSecondLastKama10
                                            && thirdLastPriceNotTouchedThirdLastKama10
                                            && fourthLastPriceNotTouchedFourthLastKama10
                                            && fifthLastPriceNotTouchedFifthLastKama10;
        
        // this is because when we open position, we are looking at the last Kama value to always above Kama14, so we need to check if the price is below the last Kama value
        var lastKama14 = kama14s.Last();
        var secondLastKama14 = kama14s.ElementAt(kama14s.Count - 2);
        var thirdLastKama14 = kama14s.ElementAt(kama14s.Count - 3);
        var fourthLastKama14 = kama14s.ElementAt(kama14s.Count - 4);
        
        var secondLastPriceTouchSecondLastKama14 = secondLastPrice.CandleRange.Intersect(new NumericRange((decimal)secondLastKama14, (decimal)secondLastKama14)) && secondLastPrice.Close < (decimal)secondLastKama14;
        var thirdLastPriceNotTouchThirdLastKama14 = !thirdLastPrice.CandleRange.Intersect(new NumericRange((decimal)thirdLastKama14, (decimal)thirdLastKama14)) || thirdLastPrice.Close > (decimal)thirdLastKama14;
        var fourthLastPriceNotTouchFourthLastKama14 = !fourthLastPrice.CandleRange.Intersect(new NumericRange((decimal)fourthLastKama14, (decimal)fourthLastKama14)) || fourthLastPrice.Close > (decimal)fourthLastKama14;
        var priceCloseBelowKama14 = price.High < (decimal)lastKama14
                                    && secondLastPriceTouchSecondLastKama14
                                    && thirdLastPriceNotTouchThirdLastKama14
                                    && fourthLastPriceNotTouchFourthLastKama14;
        
        if (lastPriceCrossedBelowLastKama10 || priceCloseBelowKama14)
        {
            var ticker = CryptosToTrade.CryptoEnumToName[crypto];
            var message = string.Empty;
            if (lastPriceCrossedBelowLastKama10)
            {
                message = $"{ticker} close ({price.Close}) below Kama10 value {lastKama10} at price's date {price.Date:yyyy-MM-dd HH:mm:ss}";
            }
            else if (priceCloseBelowKama14)
            {
                message = $"{ticker} close ({price.Close}) below Kama14 value {lastKama14} at price's date {price.Date:yyyy-MM-dd HH:mm:ss}";
            }
            
            var alert = new Alert
            {
                Ticker = ticker,
                Message = message,
                CreatedAt = price.Date,
                Strategy = nameof(PriceTouchKaufmanStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Close,
                Timeframe = parameter.Timeframe,
                PriceClosed = price.Close
            };
            ExitAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}