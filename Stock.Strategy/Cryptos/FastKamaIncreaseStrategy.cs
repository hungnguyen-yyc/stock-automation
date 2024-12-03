using Skender.Stock.Indicators;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies.Cryptos;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;

namespace Stock.Strategies;

public class FastKamaIncreaseStrategy : ICryptoStrategy
{
    public string Description => "KAMA increased by 2% in the last 4 candles";
    
    public event AlertEventHandler? EntryAlertCreated;
    
    public event AlertEventHandler? ExitAlertCreated;
    
    public void CheckForBullishEntry(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var kama = prices.GetKama(14, 2, 30).ToList();
        
        var secondLastKama = kama.ElementAt(kama.Count - 2).Kama ?? 0;
        var forthLastKama = kama.ElementAt(kama.Count - 4).Kama ?? 0;
        
        if (forthLastKama == 0)
        {
            return;
        }
        
        var incrementInPercent = ((secondLastKama / forthLastKama) - 1) * 100;
        
        if (incrementInPercent > 2)
        {
            var alert = new Alert
            {
                Ticker = CryptosToTrade.CryptoEnumToName[crypto],
                Message = $"KAMA increased by {incrementInPercent}%",
                CreatedAt = prices.Last().Date,
                Strategy = $"{nameof(FastKamaIncreaseStrategy)}",
                PriceClosed = prices.Last().Close,
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe
            };
            EntryAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
    
    public void CheckForBullishExit(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var kama = prices.GetKama(14, 2, 30).ToList();
        var secondLastKama = kama.ElementAt(kama.Count - 2).Kama ?? 0;
        var thirdLastKama = kama.ElementAt(kama.Count - 3).Kama ?? 0;
        var forthLastKama = kama.ElementAt(kama.Count - 4).Kama ?? 0;
        
        var kamaCrossedDown = secondLastKama < thirdLastKama && thirdLastKama < forthLastKama;
        
        if (kamaCrossedDown)
        {
            var alert = new Alert
            {
                Ticker = CryptosToTrade.CryptoEnumToName[crypto],
                Message = $"KAMA crossed down",
                CreatedAt = prices.Last().Date,
                Strategy = $"{nameof(FastKamaIncreaseStrategy)}",
                OrderPosition = OrderPosition.Long,
                PriceClosed = prices.Last().Close,
                PositionAction = PositionAction.Close,
                Timeframe = parameter.Timeframe
            };
            ExitAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}