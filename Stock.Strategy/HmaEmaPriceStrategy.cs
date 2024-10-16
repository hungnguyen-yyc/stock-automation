using Skender.Stock.Indicators;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public class HmaEmaPriceStrategy : IStrategy
{
    public string Description => "To heck Price At Close with Hull Moving Average and Exponential Moving Average."  +
                                 "If Price At Close Above HMA and EMA AND HMA & EMA increasing in last 5 days, then bullish. " +
                                 "And vice versa.";
    public event AlertEventHandler? AlertCreated;

    public void Run(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter parameter)
    {
        CheckForBullish(ticker, ascSortedByDatePrice, parameter);
        CheckForBearish(ticker, ascSortedByDatePrice, parameter);
    }

    private void CheckForBullish(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter parameter)
    {
        var price = ascSortedByDatePrice.Last();
        var hmas = ascSortedByDatePrice.GetHma(50).Select(x => x.Hma ?? 0).ToList();
        var emas = ascSortedByDatePrice.GetEma(50).Select(x => x.Ema ?? 0).ToList();

        var priceAboveHma = price.IsGreenCandle && price.Close > (decimal)hmas.Last();
        var priceAboveEma = price.IsGreenCandle && price.Close > (decimal)emas.Last();

        var lastNHmas = hmas.TakeLast(6).ToList();
        var lastNHmasIncreasing = false;
        var hmaDowntrendBeforeReversal = lastNHmas[0] > lastNHmas[1]; 
        for (var i = 2; i < lastNHmas.Count; i++)
        {
            if (lastNHmas[i] > lastNHmas[i - 1])
            {
                lastNHmasIncreasing = true;
            }
            else
            {
                lastNHmasIncreasing = false;
                break;
            }
        }
        
        var lastNEmas = emas.TakeLast(6).ToList();
        var lastNEmasIncreasing = false;
        var emaDowntrendBeforeReversal = lastNEmas[0] > lastNEmas[1];
        for (var i = 2; i < lastNEmas.Count; i++)
        {
            if (lastNEmas[i] > lastNEmas[i - 1])
            {
                lastNEmasIncreasing = true;
            }
            else
            {
                lastNEmasIncreasing = false;
                break;
            }
        }

        var bullishReversal = (hmaDowntrendBeforeReversal || emaDowntrendBeforeReversal) && (lastNHmasIncreasing || lastNEmasIncreasing);

        if (priceAboveEma && priceAboveHma && bullishReversal)
        {
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{nameof(HmaEmaPriceStrategy)}: Price cross above HMA50 && EMA50",
                CreatedAt = price.Date,
                Strategy = nameof(HmaEmaPriceStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe
            };
            
            AlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }

    public void CheckForBearish(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter parameter)
    {
        var price = ascSortedByDatePrice.Last();
        var hmas = ascSortedByDatePrice.GetHma(50).Select(x => x.Hma ?? 0).ToList();
        var emas = ascSortedByDatePrice.GetEma(50).Select(x => x.Ema ?? 0).ToList();
        
        var priceBelowHma = !price.IsGreenCandle && price.Close < (decimal)hmas.Last();
        var priceBelowEma = !price.IsGreenCandle && price.Close < (decimal)emas.Last();
        
        var lastNHmas = hmas.TakeLast(6).ToList();
        var lastNHmasDecreasing = false;
        var hmaUptrendBeforeReversal = lastNHmas[0] < lastNHmas[1];
        for (var i = 3; i < lastNHmas.Count; i++)
        {
            if (lastNHmas[i] < lastNHmas[i - 1])
            {
                lastNHmasDecreasing = true;
            }
            else
            {
                lastNHmasDecreasing = false;
                break;
            }
        }
        
        var lastNEmas = emas.TakeLast(6).ToList();
        var lastNEmasDecreasing = false;
        var emaUptrendBeforeReversal = lastNEmas[0] < lastNEmas[1];
        for (var i = 2; i < lastNEmas.Count; i++)
        {
            if (lastNEmas[i] < lastNEmas[i - 1])
            {
                lastNEmasDecreasing = true;
            }
            else
            {
                lastNEmasDecreasing = false;
                break;
            }
        }
        
        var bearishReversal = (hmaUptrendBeforeReversal || emaUptrendBeforeReversal) && (lastNHmasDecreasing || lastNEmasDecreasing);
        
        if (priceBelowEma && priceBelowHma && bearishReversal)
        {
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{nameof(HmaEmaPriceStrategy)}: Price cross below HMA50 && EMA50",
                CreatedAt = price.Date,
                Strategy = nameof(HmaEmaPriceStrategy),
                OrderPosition = OrderPosition.Short,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe
            };
            
            AlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}