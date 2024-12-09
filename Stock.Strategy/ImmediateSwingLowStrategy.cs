using Skender.Stock.Indicators;
using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public class ImmediateSwingLowStrategy : IStrategy
{
    public string Description { get; }
    public event AlertEventHandler? AlertCreated;
    public event AlertEventHandler? EntryAlertCreated;
    public event AlertEventHandler? ExitAlertCreated;

    public void CheckForBullishEntry(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var swingPointsParameters = (ImmediateSwingLowEntryParameter)parameter;
        var swingLows = SwingPointAnalyzer.FindSwingLows(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        var temas = prices.GetTema(swingPointsParameters.TemaPeriod).ToList();
        
        var lastPrice = prices.Last();
        var lastSwingLow = swingLows.Last();
        var indexOfSecondLastSwingLowInPrices = prices.ToList().IndexOf(lastSwingLow);
        var indexOfLastPrice = prices.Count - 1;
        var movingAwayFromLastSwingLow = indexOfLastPrice - indexOfSecondLastSwingLowInPrices == swingPointsParameters.NumberOfCandlesticksToLookBack / 2;
        var lastPriceCloseAboveLastSwingLow = lastPrice.Close > swingLows.Last().Low;
        
        var lastTema = temas.Last().Tema ?? 0;
        var secondLastTema = temas.ElementAt(temas.Count - 2).Tema ?? 0;
        var fifthLastTema = temas.ElementAt(temas.Count - 5).Tema ?? 0;

        var atr = prices.GetAtr().Select(x => x.Atr ?? 0).Last();
        
        if (movingAwayFromLastSwingLow && lastPriceCloseAboveLastSwingLow && secondLastTema < lastTema && secondLastTema > fifthLastTema && lastPrice.Close > (decimal)lastTema)
        {
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{ticker} Enter position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = prices.Last().Date,
                Strategy = nameof(ImmediateSwingLowStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Open,
                Timeframe = parameter.Timeframe,
                PriceClosed = prices.Last().Close,
                StopLoss = lastSwingLow.Low,
                TakeProfit = lastPrice.Close + (decimal)atr * 3
            };
            
            AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            EntryAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }

    public void CheckForBullishExit(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var swingPointsParameters = (ImmediateSwingLowExitParameter)parameter;
        var swingHighs = SwingPointAnalyzer.FindSwingHighs(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        
        var lastPrice = prices.Last();
        var swingHighsOrderByPriceCloseToLastSwingLow = swingHighs
            .Where(x => x.High > lastPrice.Close && (x.High / lastPrice.Close) - 1 > 0.05m) // 5% above the last price
            .ToList();
        var hasAnySwingHighs = swingHighsOrderByPriceCloseToLastSwingLow.Any();
        if (!hasAnySwingHighs)
        {
            return;
        }
        
        var targetSwingHigh = swingHighsOrderByPriceCloseToLastSwingLow.OrderBy(x => x.High - lastPrice.Close).First();
        var temas = prices.GetTema(swingPointsParameters.TemaPeriod).ToList();
        var lastTema = temas.Last().Tema ?? 0;
        
        if (lastPrice.Close >= targetSwingHigh.High || lastPrice.Close < swingPointsParameters.StopLoss || lastPrice.Close < (decimal)lastTema)
        {
            var lastPriceCloseAboveTargetSwingHigh = lastPrice.Close >= targetSwingHigh.High;
            var lastPriceCloseBelowStopLoss = lastPrice.Close < swingPointsParameters.StopLoss;
            var lastPriceCloseBelowTema = lastPrice.Close < (decimal)lastTema;
            var message = string.Empty;
            
            if (lastPriceCloseAboveTargetSwingHigh)
            {
                message = $"{nameof(ImmediateSwingLowStrategy)} {ticker} Exit position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss} because the price {lastPrice.Close} is above the target swing high {targetSwingHigh.High}";
            }
            else if (lastPriceCloseBelowStopLoss)
            {
                message = $"{nameof(ImmediateSwingLowStrategy)} {ticker} Exit position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss} because the price {lastPrice.Close} is below the stop loss {swingPointsParameters.StopLoss}";
            }
            else if (lastPriceCloseBelowTema)
            {
                message = $"{nameof(ImmediateSwingLowStrategy)} {ticker} Exit position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss} because the price {lastPrice.Close} is below the tema {lastTema}";
            }
            
            
            var alert = new Alert
            {
                Ticker = ticker,
                Message = message,
                CreatedAt = prices.Last().Date,
                Strategy = nameof(ImmediateSwingLowStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Close,
                Timeframe = parameter.Timeframe,
                PriceClosed = prices.Last().Close
            };
            
            AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            ExitAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}