using Stock.Data;
using Stock.Data.EventArgs;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Data;
using Microsoft.Toolkit.Uwp.Notifications;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;

namespace Stock.UI.Components;

public partial class MainScreenViewModel
{
    private async Task RunBackTest()
    {
        var tickers = TickersWithoutAll;
        var timeframes = new[] { Timeframe.Hour1 };
        var hmaEmaStrategy = new HmaEmaPriceStrategy();
        hmaEmaStrategy.AlertCreated += Strategy_AlertCreated;
        
        var immediateSwingLowStrategy = new ImmediateSwingLowAndSwingPointStrategy();
        immediateSwingLowStrategy.AlertCreated += Strategy_AlertCreated;
            
        foreach (var timeframe in timeframes)
        {
            _strategy.AlertCreated -= Strategy_AlertCreated;
            _strategy.TrendLineCreated -= Strategy_TrendLineCreated;
            _strategy.PivotLevelCreated -= Strategy_PivotLevelCreated;

            _strategy = new SwingPointsLiveTradingHighTimeframesStrategy();

            _strategy.AlertCreated += Strategy_AlertCreated;
            _strategy.TrendLineCreated += Strategy_TrendLineCreated;
            _strategy.PivotLevelCreated += Strategy_PivotLevelCreated;

            foreach (var ticker in tickers)
            {
                try
                {
                    var swingPointStrategyParameter = SwingPointParametersProvider.GetSwingPointStrategyParameter(ticker, timeframe);

                    IReadOnlyCollection<Price> prices;
                    if (timeframe == Timeframe.Daily)
                    {
                        prices = await _repo.GetStockDataForHighTimeframesAsc(ticker, timeframe, DateTime.Now.AddYears(-10), DateTime.Now.AddDays(1));
                    }
                    else
                    {
                        prices = await _repo.GetStockDataForHighTimeframesAsc(ticker, timeframe, DateTime.Now.AddYears(-5), DateTime.Now.AddDays(1));
                    }
                        
                    var priceToStartTesting = prices.First(x => x.Date >= DateTime.Now.AddMonths(-5));
                        
                    var index = 0;
                    for (int i = 0; i < prices.Count; i++)
                    {
                        var price = prices.ElementAt(i);
                        if (price.Date == priceToStartTesting.Date)
                        {
                            index = i;
                            break;
                        }
                    }
                        
                    for (int i = index; i < prices.Count; i++)
                    {
                        _tickerAndPrices[ticker] = prices.Take(i).ToList();
                        UpdateFilteredTrendLines(ticker);
                        var hmaEmaStrategyParameter = new HmaEmaPriceStrategyParameter
                        {
                            Timeframe = timeframe,
                        };
                            
                        await Task.Run(() =>
                        {
                            // hmaEmaStrategy.Run(ticker, prices.Take(i).ToList(), hmaEmaStrategyParameter);
                            /*_strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            _strategy.CheckForTouchingDownTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            _strategy.CheckForTouchingUpTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);*/
                            
                            var immediateSwingLowEntryParameter = ImmediateSwingLowParameterProvider.GetEntryParameter(ticker);
                            immediateSwingLowEntryParameter.Timeframe = timeframe;
                            immediateSwingLowEntryParameter.NumberOfCandlesticksToLookBack = 30;
                            var immediateSwingLowExitParameter = ImmediateSwingLowParameterProvider.GetExitParameter(ticker);
                            immediateSwingLowExitParameter.Timeframe = timeframe;
                            immediateSwingLowExitParameter.NumberOfCandlesticksToLookBack = 30;
                            immediateSwingLowStrategy.CheckForBullishEntry(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            //immediateSwingLowStrategy.CheckForBullishExit(ticker, prices.Take(i).ToList(), immediateSwingLowExitParameter);
                        });
                    }
                    Logs.Add(new LogEventArg($"Finished running strategy for {ticker} {timeframe} at {DateTime.Now}"));
                }
                catch (Exception ex)
                {
                    Logs.Add(new LogEventArg(ex.Message));
                }
            }
        }

        Logs.Add(new LogEventArg($"Finished running strategy at {DateTime.Now}"));
    }
}