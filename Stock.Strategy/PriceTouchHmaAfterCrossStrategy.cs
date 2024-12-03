using Skender.Stock.Indicators;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;

namespace Stock.Strategies;

public class PriceTouchHmaAfterCrossStrategy : IStrategy
{
    public string Description => "To check when Price touched HMAs after cross, will it continue to go up or down.";
    public event AlertEventHandler? EntryAlertCreated;
    public event AlertEventHandler? ExitAlertCreated;

    public void Run(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        CheckForBullishEntry(ticker, prices, parameter);
    }

    private void CheckForBullishEntry(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var price = prices.Last();
        var secondLastPrice = prices.ElementAt(prices.Count - 2);
        var hmas = prices.GetHma(50).Select(x => x.Hma ?? 0).ToList();
        var secondLastHma = hmas.ElementAt(hmas.Count - 2);
        var numberOfPricesAboveHma = 3;
        var trailingBackIndex = prices.Count - 3;
        var trailingBackLimit = 5;
        var touchFromAbove = false;
        
        // first check if second last price touched or intersected with HMA
        if (secondLastPrice.CandleRange.Intersect(new NumericRange((decimal)secondLastHma, (decimal)secondLastHma)))
        {
            // then trailing back to see if there are 3/5 prices above HMA
            while (numberOfPricesAboveHma > 0 && trailingBackLimit > 0)
            {
                var p = prices.ElementAt(trailingBackIndex);
                var hma = hmas.ElementAt(trailingBackIndex);
                var hmaRange = new NumericRange((decimal)hma, (decimal)hma);

                if (p.CandleRange.Intersect(hmaRange))
                {
                    trailingBackIndex--;
                    trailingBackLimit--;
                    continue;
                }
            
                if (p.Close > (decimal)hma)
                {
                    numberOfPricesAboveHma--;
                    trailingBackIndex--;
                }
            
                if (p.Close < (decimal)hma)
                {
                    break;
                }
            }
            
            touchFromAbove = numberOfPricesAboveHma == 0;
        }
        
        // then check if the current price is completely above HMA
        touchFromAbove = touchFromAbove && price.Close > (decimal)hmas.Last() && !price.CandleRange.Intersect(new NumericRange((decimal)hmas.Last(), (decimal)hmas.Last()));

        if (touchFromAbove)
        {
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{nameof(PriceTouchHmaAfterCrossStrategy)}: Price bounced off HMA (50).",
                CreatedAt = price.Date,
                Strategy = nameof(PriceTouchHmaAfterCrossStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe
            };
            
            EntryAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
    
}