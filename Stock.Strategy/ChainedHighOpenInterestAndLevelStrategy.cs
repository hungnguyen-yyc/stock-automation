using Stock.Data;
using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public class ChainedHighOpenInterestAndLevelStrategy : IStrategy
{
    private readonly StockDataRetrievalService _stockDataRetrievalService;
    private HighChangeInOpenInterestStrategy _highChangeInOpenInterestStrategy;
    private HmaEmaPriceStrategy _hmaEmaPriceStrategy;
    private SwingPointsLiveTradingHighTimeframesStrategy _swingPointsLiveTradingHighTimeframesStrategy;
    private readonly ImmediateSwingLowStrategy _immediateSwingLowStrategy;

    public string Description => "Chained High Open Interest and Level Strategy";
    public event AlertEventHandler? AlertCreated;

    public ChainedHighOpenInterestAndLevelStrategy(StockDataRetrievalService stockDataRetrievalService,
        HighChangeInOpenInterestStrategy highChangeInOpenInterestStrategy)
    {
        _stockDataRetrievalService = stockDataRetrievalService;
        _highChangeInOpenInterestStrategy = highChangeInOpenInterestStrategy;
        _swingPointsLiveTradingHighTimeframesStrategy = new SwingPointsLiveTradingHighTimeframesStrategy();
        _hmaEmaPriceStrategy = new HmaEmaPriceStrategy();
        _immediateSwingLowStrategy = new ImmediateSwingLowStrategy();
        
        _highChangeInOpenInterestStrategy.AlertCreated += HighChangeInOpenInterestStrategyOnAlertCreated;
        _swingPointsLiveTradingHighTimeframesStrategy.AlertCreated += SwingPointsLiveTradingHighTimeframesStrategyOnAlertCreated;
        _hmaEmaPriceStrategy.AlertCreated += HmaEmaPriceStrategyOnAlertCreated;
        _immediateSwingLowStrategy.AlertCreated += ImmediateSwingLowStrategyOnAlertCreated;
    }

    private void ImmediateSwingLowStrategyOnAlertCreated(object sender, AlertEventArgs e)
    {
        var alert = new Alert
        {
            Ticker = e.Alert.Ticker,
            Timeframe = e.Alert.Timeframe,
            CreatedAt = e.Alert.CreatedAt,
            OrderPosition = e.Alert.OrderPosition,
            Message = $"High Option Interest With {e.Alert.Message}"
        };
        AlertCreated?.Invoke(this, new AlertEventArgs(alert));
    }

    private void HmaEmaPriceStrategyOnAlertCreated(object sender, AlertEventArgs e)
    {
        var alert = new Alert
        {
            Ticker = e.Alert.Ticker,
            Timeframe = e.Alert.Timeframe,
            CreatedAt = e.Alert.CreatedAt,
            OrderPosition = e.Alert.OrderPosition,
            Message = $"High Option Interest With {e.Alert.Message}"
        };
        AlertCreated?.Invoke(this, new AlertEventArgs(alert));
    }

    private void SwingPointsLiveTradingHighTimeframesStrategyOnAlertCreated(object sender, AlertEventArgs e)
    {
        var alert = new Alert
        {
            Ticker = e.Alert.Ticker,
            Timeframe = e.Alert.Timeframe,
            CreatedAt = e.Alert.CreatedAt,
            OrderPosition = e.Alert.OrderPosition,
            Message = $"High Option Interest With {e.Alert.Message}"
        };
        AlertCreated?.Invoke(this, new AlertEventArgs(alert));
    }

    private void HighChangeInOpenInterestStrategyOnAlertCreated(object sender, AlertEventArgs e)
    {
        if (e.Alert is not HighChangeInOpenInterestStrategyAlert highChangeInOpenInterestStrategyAlert) return;
        
        var ticker = highChangeInOpenInterestStrategyAlert.Ticker;
        var timeframe = highChangeInOpenInterestStrategyAlert.Timeframe;
        
        Task.Run(async () => {
            var prices = await _stockDataRetrievalService.GetStockDataForHighTimeframesAsc(ticker, timeframe, DateTime.Now.AddYears(-10), DateTime.Now.AddDays(1));
            var swingPointStrategyParameter = SwingPointParametersProvider.GetSwingPointStrategyParameter(ticker, timeframe);
            _swingPointsLiveTradingHighTimeframesStrategy.CheckForTopBottomTouch(ticker, prices.ToList(), swingPointStrategyParameter);
            _swingPointsLiveTradingHighTimeframesStrategy.CheckForTouchingDownTrendLine(ticker, prices.ToList(), swingPointStrategyParameter);
            _swingPointsLiveTradingHighTimeframesStrategy.CheckForTouchingUpTrendLine(ticker, prices.ToList(), swingPointStrategyParameter);
            
            var immediateSwingLowEntryParameter = new ImmediateSwingLowEntryParameter
            {
                NumberOfCandlesticksToLookBack = 10,
                TemaPeriod = 200,
                Timeframe = timeframe,
            };
            var immediateSwingLowExitParameter = new ImmediateSwingLowExitParameter
            {
                NumberOfCandlesticksToLookBack = 100,
                TemaPeriod = 150,
                Timeframe = timeframe,
                StopLoss = 0,
                TakeProfit = int.MaxValue
            };
            _immediateSwingLowStrategy.CheckForBullishEntry(ticker, prices.ToList(), immediateSwingLowEntryParameter);
            _immediateSwingLowStrategy.CheckForBullishExit(ticker, prices.ToList(), immediateSwingLowExitParameter);
            
            var hmaEmaStrategyParameter = new HmaEmaPriceStrategyParameter
            {
                Timeframe = timeframe,
            };
            _hmaEmaPriceStrategy.Run(ticker, prices.ToList(), hmaEmaStrategyParameter);
        });
    }
}