using Newtonsoft.Json;
using Stock.Data;
using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;
using Stock.Strategy;

namespace Stock.Strategies;

public sealed class HighChangeInOpenInterestStrategy : IStrategy
{
    private const string FilePath = "OptionsScreeningResults";
    private const string FileExtension = ".json";
    private readonly StockDataRetrievalService _stockDataRetrievalService;
    
    public static OptionsScreeningParams OptionsScreeningParams = OptionsScreeningParams.Default;

    public HighChangeInOpenInterestStrategy(StockDataRetrievalService stockDataRetrievalService)
    {
        _stockDataRetrievalService = stockDataRetrievalService;
    }
    
    public string Description => "High Change in Open Interest";
    
    public event AlertEventHandler AlertCreated;
    
    public async Task RunAgainstPreviousDay(OptionsScreeningParams requestParams, double percentageChange)
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
                var optionData = todayOption;
                var optionType = optionData.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? "C" : "P";
                var newYorkTimeString = $"{optionData.TradeTime:yyyy-MM-dd HH:mm:ss} (EST)";
                var message =
                    $"{optionData.UnderlyingSymbol}|{optionData.ExpirationDateFormatted}|{optionData.Strike}{optionType}: Open Interest: {optionData.OpenInterest} ({Math.Round(change, 2)}%) | Trade time: {newYorkTimeString}";
                CreateAndInvokeAlert(todayOption, message);
            }
        }
    }
    
    public async Task RunAgainstPreviousSnapshot(OptionsScreeningParams requestParams, double percentageChange)
    {
        var secondLatestSnapshot = await LoadLatestOptionScreeningResults(requestParams);
        
        await UpdateOptionScreeningResultsToAppData(requestParams);
        var latestSnapshot = await LoadLatestOptionScreeningResults(requestParams);
        
        foreach (var latestOptionData in latestSnapshot)
        {
            var secondLatestOptionData = secondLatestSnapshot.FirstOrDefault(o 
                => o.UnderlyingSymbol == latestOptionData.UnderlyingSymbol 
                   && o.ExpirationDate == latestOptionData.ExpirationDate 
                   && o.Strike == latestOptionData.Strike 
                   && o.Type == latestOptionData.Type);
            if (secondLatestOptionData == null)
            {
                continue;
            }

            var change = (double)(latestOptionData.OpenInterest - secondLatestOptionData.OpenInterest) / secondLatestOptionData.OpenInterest * 100;
            if (latestOptionData.OpenInterest != secondLatestOptionData.OpenInterest)
            {
                var optionData = latestOptionData;
                var optionType = optionData.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? "C" : "P";
                var message =
                    $"{optionData.UnderlyingSymbol}|{optionData.ExpirationDateFormatted}|{optionData.Strike}{optionType}: OI: {optionData.OpenInterest} ({Math.Round(change, 2)}%) | Prev: {secondLatestOptionData.OpenInterest} (at {secondLatestOptionData.TradeTime:yyyy-MM-dd HH:mm:ss} EST)";
                CreateAndInvokeAlert(latestOptionData, message);
            }
        }
    }
    
    private void CreateAndInvokeAlert(OptionsScreeningResult optionData, string message)
    {
        var alert = new HighChangeInOpenInterestStrategyAlert();
        var optionType = optionData.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? "C" : "P";
        var optionTicker = $"{optionData.UnderlyingSymbol}|{optionData.ExpirationDate:yyyyMMdd}|{optionData.Strike}{optionType}";
        var orderPosition = optionData.Type.Equals("call", StringComparison.InvariantCultureIgnoreCase) ? OrderPosition.Long : OrderPosition.Short;
        
        alert.Ticker = $"{optionData.UnderlyingSymbol}";
        alert.Timeframe = Timeframe.Daily;
        alert.CreatedAt = optionData.TradeTime.DateTime;
        alert.OrderPosition = orderPosition;
        alert.OptionTicker = optionTicker;
        alert.OpenInterest = optionData.OpenInterest;
        alert.Message = message;
        AlertCreated?.Invoke(this, new AlertEventArgs(alert));
    }
    
    private async Task UpdateOptionScreeningResultsToAppData(OptionsScreeningParams requestParams)
    {
        var fileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}{FileExtension}";
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FilePath);
        var useEod = false;
        
        if (!Directory.Exists(directory))
        {
            useEod = true;
            Directory.CreateDirectory(directory);
        }
        
        var optionsScreeningResults = await _stockDataRetrievalService.GetOptionsScreeningResults(requestParams, useEod);
        var json = JsonConvert.SerializeObject(optionsScreeningResults);
        var path = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(path, json);
    }
    
    private async Task<List<OptionsScreeningResult>> LoadLatestOptionScreeningResults(OptionsScreeningParams requestParams)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FilePath);
        
        if (!Directory.Exists(directory))
        {
            await UpdateOptionScreeningResultsToAppData(requestParams);
        }
        
        var files = Directory.GetFiles(directory);
        if (files.Length == 0)
        {
            return new List<OptionsScreeningResult>();
        }
        
        var sortedFiles = files
            .Select(fileName => new { FileName = fileName, Timestamp = long.Parse(Path.GetFileNameWithoutExtension(fileName)) })
            .OrderByDescending(file => file.Timestamp)
            .Select(file => file.FileName)
            .ToList();
        
        var latestFile = sortedFiles.First();
        var json = await File.ReadAllTextAsync(latestFile);
        var result = JsonConvert.DeserializeObject<List<OptionsScreeningResult>>(json);
        
        if (result == null)
        {
            return new List<OptionsScreeningResult>();
        }
        
        return result;
    }
}