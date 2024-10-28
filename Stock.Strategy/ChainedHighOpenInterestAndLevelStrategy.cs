using Stock.Data;
using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public class ChainedHighOpenInterestAndLevelStrategyParameters
{
    public ChainedHighOpenInterestAndLevelStrategyParameters(IReadOnlyCollection<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter, OptionsScreeningParams requestParams, double percentageChange)
    {
        AscSortedByDatePrice = ascSortedByDatePrice;
        StrategyParameter = strategyParameter;
        RequestParams = requestParams;
        PercentageChange = percentageChange;
    }

    public IReadOnlyCollection<Price> AscSortedByDatePrice { get; }
    public IStrategyParameter StrategyParameter { get; }
    public OptionsScreeningParams RequestParams { get; }
    public double PercentageChange { get; }
}


public class ChainedHighOpenInterestAndLevelStrategy : IStrategy
{
    private readonly StockDataRetrievalService _stockDataRetrievalService;
    private HighChangeInOpenInterestStrategy _highChangeInOpenInterestStrategy;
    private SwingPointsLiveTradingHighTimeframesStrategy _swingPointsLiveTradingHighTimeframesStrategy;
    
    public string Description => "Chained High Open Interest and Level Strategy";
    public event AlertEventHandler? AlertCreated;

    public ChainedHighOpenInterestAndLevelStrategy(StockDataRetrievalService stockDataRetrievalService,
        HighChangeInOpenInterestStrategy highChangeInOpenInterestStrategy)
    {
        _stockDataRetrievalService = stockDataRetrievalService;
        _highChangeInOpenInterestStrategy = highChangeInOpenInterestStrategy;
        _swingPointsLiveTradingHighTimeframesStrategy = new SwingPointsLiveTradingHighTimeframesStrategy();
        
        _highChangeInOpenInterestStrategy.AlertCreated += HighChangeInOpenInterestStrategyOnAlertCreated;
        _swingPointsLiveTradingHighTimeframesStrategy.AlertCreated += SwingPointsLiveTradingHighTimeframesStrategyOnAlertCreated;
    }

    private void SwingPointsLiveTradingHighTimeframesStrategyOnAlertCreated(object sender, AlertEventArgs e)
    {
        var alert = new Alert
        {
            Ticker = e.Alert.Ticker,
            Timeframe = e.Alert.Timeframe,
            CreatedAt = e.Alert.CreatedAt,
            OrderPosition = e.Alert.OrderPosition,
            Message = $"{nameof(ChainedHighOpenInterestAndLevelStrategy)}: High Option Interest With {e.Alert.Message}"
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
        });
    }
}