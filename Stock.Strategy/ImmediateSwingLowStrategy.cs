using Stock.Shared.Models;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public class ImmediateSwingLowStrategy : ICryptoStrategy
{
    public string Description { get; }
    public event AlertEventHandler? EntryAlertCreated;
    public event AlertEventHandler? ExitAlertCreated;
    
    public void Run(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        CheckForBullishEntry(ticker, prices, parameter);
        CheckForBullishExit(ticker, prices, parameter);
    }

    private void CheckForBullishEntry(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var swingPointsParameters = (SwingPointStrategyParameter)parameter;
        var swingLows = SwingPointAnalyzer.FindSwingLows(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        var swingHighs = SwingPointAnalyzer.FindSwingHighs(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        
        var lastPrice = prices.Last();
        var movingAwayFromLastSwingLow = lastPrice.Date.Subtract(swingLows.Last().Date).Hours == swingPointsParameters.NumberOfCandlesticksToLookBack;
        var lastPriceCloseAboveLastSwingLow = lastPrice.Close > swingLows.Last().Low;
        
        if (movingAwayFromLastSwingLow && lastPriceCloseAboveLastSwingLow)
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
                PriceClosed = prices.Last().Close
            };
            
            EntryAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }

    private void CheckForBullishExit(string ticker, IReadOnlyCollection<Price> prices, IStrategyParameter parameter)
    {
        var swingPointsParameters = (SwingPointStrategyParameter)parameter;
        var swingLows = SwingPointAnalyzer.FindSwingLows(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        var swingHighs = SwingPointAnalyzer.FindSwingHighs(prices.ToList(), swingPointsParameters.NumberOfCandlesticksToLookBack).OrderBy(x => x.Date).ToList();
        
        var lastSwingLow = swingLows.Last();
        var swingHighsOrderByPriceCloseToLastSwingLow = swingHighs.OrderBy(x => x.High - lastSwingLow.Low).ToList();
        
        var lastPrice = prices.Last();
        var secondLastHigh = swingHighsOrderByPriceCloseToLastSwingLow.ElementAt(swingHighsOrderByPriceCloseToLastSwingLow.Count - 2);
        
        if (lastPrice.Close >= secondLastHigh.High || lastPrice.Close <= lastSwingLow.Low)
        {
            var alert = new Alert
            {
                Ticker = ticker,
                Message = $"{ticker} Exit position at {prices.Last().Close} at {prices.Last().Date:yyyy-MM-dd HH:mm:ss}",
                CreatedAt = prices.Last().Date,
                Strategy = nameof(ImmediateSwingLowStrategy),
                OrderPosition = OrderPosition.Long,
                PositionAction = PositionAction.Close,
                Timeframe = parameter.Timeframe,
                PriceClosed = prices.Last().Close
            };
            
            ExitAlertCreated?.Invoke(this, new AlertEventArgs(alert));
        }
    }
}