using Skender.Stock.Indicators;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies.Cryptos;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;

namespace Stock.Strategies;

public class TEMATrendFollowingStrategy : ICryptoStrategy
{
    public string Description => "TEMA Trend Following Strategy";
    public event AlertEventHandler? EntryAlertCreated;
    public event AlertEventHandler? ExitAlertCreated;
    
    public void CheckForBullishEntry(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var temas = prices.GetTema(50).Select(x => x.Tema ?? 0).ToList();
        var isLast4TemaUp = temas.Last() > temas.ElementAt(temas.Count - 2) && temas.ElementAt(temas.Count - 2) > temas.ElementAt(temas.Count - 3) && temas.ElementAt(temas.Count - 3) > temas.ElementAt(temas.Count - 4);
        var isLast7To4TemaDown = temas.ElementAt(temas.Count - 4) < temas.ElementAt(temas.Count - 5) && temas.ElementAt(temas.Count - 5) < temas.ElementAt(temas.Count - 6) && temas.ElementAt(temas.Count - 6) < temas.ElementAt(temas.Count - 7);
        
        if (isLast4TemaUp && isLast7To4TemaDown)
        {
            var ticker = CryptosToTrade.CryptoEnumToName[crypto];
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{ticker} Enter position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = prices.Last().Date,
                Strategy = nameof(TEMATrendFollowingStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe,
                PriceClosed = prices.Last().Close
            };
            
            EntryAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
    
    public void CheckForBullishExit(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var temas = prices.GetTema(50).Select(x => x.Tema ?? 0).ToList();
        var isLast4TemaDown = temas.Last() < temas.ElementAt(temas.Count - 2) && temas.ElementAt(temas.Count - 2) < temas.ElementAt(temas.Count - 3) && temas.ElementAt(temas.Count - 3) < temas.ElementAt(temas.Count - 4);
        var lastPriceCloseBelowLastTema = prices.Last().Close < (decimal)temas.Last();
        
        if (isLast4TemaDown || lastPriceCloseBelowLastTema)
        {
            var ticker = CryptosToTrade.CryptoEnumToName[crypto];
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{ticker} Exit position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = prices.Last().Date,
                Strategy = nameof(TEMATrendFollowingStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Close,
                Timeframe = parameter.Timeframe,
                PriceClosed = prices.Last().Close
            };
            
            ExitAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}