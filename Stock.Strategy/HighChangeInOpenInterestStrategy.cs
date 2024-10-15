using Stock.Data;
using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;
using Stock.Strategy;

namespace Stock.Strategies;

public sealed class HighChangeInOpenInterestStrategy : IStrategy
{
    private readonly StockDataRetrievalService _stockDataRetrievalService;
    
    public static OptionsScreeningParams OptionsScreeningParams = OptionsScreeningParams.Default;

    public HighChangeInOpenInterestStrategy(StockDataRetrievalService stockDataRetrievalService)
    {
        _stockDataRetrievalService = stockDataRetrievalService;
    }
    
    public string Description => "High Change in Open Interest";
    
    public event AlertEventHandler AlertCreated;
    
    public async Task Run(OptionsScreeningParams requestParams, double percentageChange)
    {
        var todayOptions = await _stockDataRetrievalService.GetOptionsScreeningResults(requestParams, false); // intraday
        var eodOptions = await _stockDataRetrievalService.GetOptionsScreeningResults(requestParams, true); // end of day
        
        foreach (var todayOption in todayOptions)
        {
            var eodOption = eodOptions.FirstOrDefault(o 
                => o.UnderlyingSymbol == todayOption.UnderlyingSymbol 
                   && o.ExpirationDate == todayOption.ExpirationDate 
                   && o.Strike == todayOption.Strike 
                   && o.Type == todayOption.Type);
            if (eodOption == null)
            {
                continue;
            }

            var change = (double)(todayOption.OpenInterest - eodOption.OpenInterest) / eodOption.OpenInterest * 100;
            if (change >= percentageChange)
            {
                var alert = new HighChangeInOpenInterestStrategyAlert();
                var optionType = todayOption.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? "C" : "P";
                var optionTicker = $"{todayOption.UnderlyingSymbol}|{todayOption.ExpirationDate:yyyyMMdd}|{todayOption.Strike}{optionType}";
                var orderPosition = todayOption.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? OrderPosition.Long : OrderPosition.Short;
                
                var newYorkTimeString = $"{todayOption.TradeTime:yyyy-MM-dd HH:mm:ss} (EST)";
                
                alert.Ticker = $"{todayOption.UnderlyingSymbol}";
                alert.Timeframe = Timeframe.Daily;
                alert.CreatedAt = DateTime.Now.Date;
                alert.OrderPosition = orderPosition;
                alert.OptionTicker = optionTicker;
                alert.Message = $"{todayOption.UnderlyingSymbol}|{todayOption.ExpirationDateFormatted}|{todayOption.Strike}{optionType}: Open Interest: {todayOption.OpenInterest} ({Math.Round(change, 2)}%) | Trade time: {newYorkTimeString}";
                AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            }
        }
    }
}