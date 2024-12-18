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
using Newtonsoft.Json;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;

namespace Stock.UI.Components;

public partial class MainScreenViewModel
{
    private async Task RunBackTest()
    {
        var highChangeInOpenInterestStrategy = new HighChangeInOpenInterestStrategy(_repo);
        highChangeInOpenInterestStrategy.AlertCreated += Strategy_AlertCreated;
            
        Tickers.Clear();
        Tickers.Add(ALL);
            
        while (true)
        {
            var secondsToWait = 60;
            var minuteModule = DateTime.Now.Second % secondsToWait;
            if (minuteModule != 0)
            {
                var timeToDelay = Math.Max(secondsToWait - minuteModule, 0);
                await Task.Delay(TimeSpan.FromSeconds(timeToDelay));
            }
                
            try
            {
                await Task.Run(() => {
                    var screeningParams = HighChangeInOpenInterestStrategy.OptionsScreeningParams;
                    highChangeInOpenInterestStrategy.RunAgainstPreviousDay(screeningParams, 20);
                    highChangeInOpenInterestStrategy.RunAgainstPreviousSnapshot(screeningParams, 2);
                });
            }
            catch (Exception ex)
            {
                Logs.Add(new LogEventArg(ex.Message));
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
        /*var tickers = TickersWithoutAll;
        Tickers.Clear();
        var timeframes = new[] { Timeframe.Daily };
        var hmaEmaStrategy = new HmaEmaPriceStrategy();
        //hmaEmaStrategy.AlertCreated += Strategy_AlertCreated;
        
        var highOpenInterestStrategy = new HighChangeInOpenInterestStrategy(_repo);
        //highOpenInterestStrategy.AlertCreated += Strategy_AlertCreated;
        
        var optionScreeningFolder = "OptionsScreeningResults";
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var optionScreeningPath = Path.Combine(localAppData, optionScreeningFolder);
        
        // read all files in the directory
        var files = Directory.GetFiles(optionScreeningPath);
        var sortedFiles = files
            .Select(fileName => new { FileName = fileName, Timestamp = long.Parse(Path.GetFileNameWithoutExtension(fileName)) })
            .OrderByDescending(file => file.Timestamp)
            .Select(file => file.FileName)
            .ToList();

        try
        {
            for (var fileIndex = 1; fileIndex < sortedFiles.Count; fileIndex++)
            {
                var current = sortedFiles[fileIndex - 1];
                var previous = sortedFiles[fileIndex];
            
                var currentContent = await File.ReadAllTextAsync(current);
                var previousContent = await File.ReadAllTextAsync(previous);
            
                var currentResults = JsonConvert.DeserializeObject<List<OptionsScreeningResult>>(currentContent);
                var previousResults = JsonConvert.DeserializeObject<List<OptionsScreeningResult>>(previousContent);

                for (var optionIndex = 0; optionIndex < currentResults.Count; optionIndex++)
                {
                    var currentResult = currentResults[optionIndex];
                    var previousResult = previousResults
                        .FirstOrDefault(x 
                            => x.UnderlyingSymbol == currentResult.UnderlyingSymbol
                               && x.Strike == currentResult.Strike
                               && x.ExpirationDate == currentResult.ExpirationDate
                               && x.Type == currentResult.Type);
                    
                    var tests = previousResults
                        .Where(x 
                            => x.UnderlyingSymbol == currentResult.UnderlyingSymbol
                               && x.Strike == currentResult.Strike
                               && x.ExpirationDate == currentResult.ExpirationDate);

                    if (previousResult != null)
                    {
                        if (previousResult.OpenInterest != currentResult.OpenInterest)
                        {
                            Console.WriteLine($"Previous {Path.GetFileNameWithoutExtension(previous)}: {previousResult.UnderlyingSymbol}|{previousResult.Strike}{previousResult.Type.ToUpper().Substring(0, 1)}|{previousResult.ExpirationDateFormatted}: {previousResult.OpenInterest} at {previousResult.TradeTime}");
                            Console.WriteLine($"Current  {Path.GetFileNameWithoutExtension(current)}: {currentResult.UnderlyingSymbol}|{currentResult.Strike}{currentResult.Type.ToUpper().Substring(0, 1)}|{currentResult.ExpirationDateFormatted}: {currentResult.OpenInterest} at {currentResult.TradeTime}");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }*/
            
        /*foreach (var timeframe in timeframes)
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
                            // _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            //_strategy.CheckForTouchingDownTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            //_strategy.CheckForTouchingUpTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                        });
                    }
                    Logs.Add(new LogEventArg($"Finished running strategy for {ticker} {timeframe} at {DateTime.Now}"));
                }
                catch (Exception ex)
                {
                    Logs.Add(new LogEventArg(ex.Message));
                }
            }
        }*/

        Logs.Add(new LogEventArg($"Finished running strategy at {DateTime.Now}"));
    }
}