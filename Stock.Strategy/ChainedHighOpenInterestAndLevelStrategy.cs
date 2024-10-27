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
    private HighChangeInOpenInterestStrategy _highChangeInOpenInterestStrategy;
    private SwingPointsLiveTradingHighTimeframesStrategy _swingPointsLiveTradingHighTimeframesStrategy;
    
    public string Description => "Chained High Open Interest and Level Strategy";
    public event AlertEventHandler? AlertCreated;
    
    public async Task Run(StockDataRetrievalService stockDataRetrievalService, ChainedHighOpenInterestAndLevelStrategyParameters requestParams)
    {
        if (_highChangeInOpenInterestStrategy != null)
        {
            _highChangeInOpenInterestStrategy.AlertCreated -= HighChangeInOpenInterestStrategyOnAlertCreated;
        }
        
        if (_swingPointsLiveTradingHighTimeframesStrategy != null)
        {
            _swingPointsLiveTradingHighTimeframesStrategy.AlertCreated -= HighChangeInOpenInterestStrategyOnAlertCreated;
        }
    }

    private void HighChangeInOpenInterestStrategyOnAlertCreated(object sender, AlertEventArgs e)
    {
        throw new NotImplementedException();
    }
}