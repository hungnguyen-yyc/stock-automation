using Stock.Data;
using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;

namespace Stock.Strategies;

// TODO: fix and consolidate strategy interface
public class HighChangeInOpenInterestStrategy
{
    private readonly StockDataRepository _stockDataRepository;

    public HighChangeInOpenInterestStrategy(StockDataRepository stockDataRepository)
    {
        _stockDataRepository = stockDataRepository;
    }
    
    public string Description => "High Change in Open Interest";
    
    public event AlertEventHandler AlertCreated;
    
    public async Task Run(OptionsScreeningParams requestParams, double percentageChange)
    {
        var todayOptions = await _stockDataRepository.GetOptionsScreeningResults(requestParams, false); // intraday
        var eodOptions = await _stockDataRepository.GetOptionsScreeningResults(requestParams, true); // end of day
        
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

            var change = (todayOption.OpenInterest - eodOption.OpenInterest) / eodOption.OpenInterest * 100;
            if (change >= percentageChange)
            {
                var alert = new Alert();
                var optionType = todayOption.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? "C" : "P";
                alert.Ticker = $"{todayOption.UnderlyingSymbol}|{todayOption.ExpirationDateFormatted}|{todayOption.Strike}|{optionType}";
                alert.Timeframe = Timeframe.Daily;
                alert.CreatedAt = DateTime.Now;
                alert.OrderPosition = OrderPosition.Long;
                alert.Message = $"High Change in Open Interest: {todayOption.UnderlyingSymbol} {todayOption.Type} {todayOption.Strike} {todayOption.ExpirationDateFormatted} {change}%";
                AlertCreated?.Invoke(this, new AlertEventArgs(alert));
            }
        }
    }
}