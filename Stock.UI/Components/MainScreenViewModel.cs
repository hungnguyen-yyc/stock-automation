using IBApi;
using Stock.Data;
using Stock.Data.EventArgs;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Shared.Models.IBKR.Messages;
using Stock.Strategies;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using Microsoft.Toolkit.Uwp.Notifications;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.EventArgs;

namespace Stock.UI.Components
{
    public class MainScreenViewModel : INotifyPropertyChanged
    {
        private const string ALL = "All";

        private readonly StockDataRepository _repo;
        private ISwingPointStrategy _strategy;
        private string _selectedTimeframe;
        private string _selectedTicker;
        private string _selectedOptionType;
        private ObservableCollection<Alert> _allAlerts;
        private ObservableCollection<Alert> _filteredAlerts;
        private ObservableCollection<string> _allOptionChain;
        private ObservableCollection<string> _filteredOptionChain;
        private ObservableCollection<OptionPrice> _optionPrices;
        private readonly object _lock = new();
        private ObservableCollection<CompletedOrderMessage> _completedOrders;
        private ObservableCollection<Tuple<string, string>> _accountSummary;
        private ObservableCollection<PositionMessage> _accountPosition;
        private ObservableCollection<TrendLine> _allTrendLines;
        private ObservableCollection<TrendLine> _filteredTrendLines;
        private ObservableCollection<OptionsScreeningResult> _allOptionsScreeningResults;
        private ObservableCollection<OptionsScreeningResult> _filteredOptionsScreeningResults;

        private Dictionary<string, IReadOnlyCollection<Price>> _tickerAndPrices;

        public MainScreenViewModel(StockDataRepository repo, ISwingPointStrategy strategy)
        {
            _repo = repo;
            _strategy = strategy;
            _selectedTimeframe = ALL;
            _selectedTicker = ALL;
            _selectedOptionType = ALL;
            _tickerAndPrices = new Dictionary<string, IReadOnlyCollection<Price>>();
            _allAlerts = new ObservableCollection<Alert>();
            _filteredAlerts = new ObservableCollection<Alert>();
            _allTrendLines = new ObservableCollection<TrendLine>();
            _filteredTrendLines = new ObservableCollection<TrendLine>();
            BindingOperations.EnableCollectionSynchronization(_filteredTrendLines, _lock);
            BindingOperations.EnableCollectionSynchronization(_filteredAlerts, _lock);

            Logs = new ObservableCollection<LogEventArg>();
            BindingOperations.EnableCollectionSynchronization(Logs, _lock);

            _allOptionChain = new ObservableCollection<string>();
            _filteredOptionChain = new ObservableCollection<string>();
            _optionPrices = new ObservableCollection<OptionPrice>();
            BindingOperations.EnableCollectionSynchronization(_filteredOptionChain, _lock);
            BindingOperations.EnableCollectionSynchronization(_optionPrices, _lock);

            _accountSummary = new ObservableCollection<Tuple<string, string>>();
            _accountPosition = new ObservableCollection<PositionMessage>();

            BindingOperations.EnableCollectionSynchronization(_accountSummary, _lock);
            BindingOperations.EnableCollectionSynchronization(_accountPosition, _lock);

            _completedOrders = new ObservableCollection<CompletedOrderMessage>();
            BindingOperations.EnableCollectionSynchronization(_completedOrders, _lock);
            
            _allOptionsScreeningResults = new ObservableCollection<OptionsScreeningResult>();
            _filteredOptionsScreeningResults = new ObservableCollection<OptionsScreeningResult>();
            BindingOperations.EnableCollectionSynchronization(_filteredOptionsScreeningResults, _lock);

            _repo.LogCreated += (message) =>
            {
                Logs.Add(message);
            };

            Tickers = new ObservableCollection<string>
            {
                ALL
            };

            foreach (var ticker in TickersToTrade.POPULAR_TICKERS)
            {
                Tickers.Add(ticker);
            }

            Timeframes = new ObservableCollection<string>
            {
                ALL,
                Timeframe.Minute15.ToString(),
                Timeframe.Minute30.ToString(),
                Timeframe.Hour1.ToString(),
                Timeframe.Daily.ToString()
            };

            StartStrategy();
        }
        
        private IReadOnlyCollection<string> TickersWithoutAll => Tickers.Where(x => x != ALL).ToList();
        
        public ObservableCollection<string> AllOptionChain => _filteredOptionChain;

        public ObservableCollection<OptionPrice> OptionPrices => _optionPrices;

        public ObservableCollection<string> Timeframes { get; }

        public ObservableCollection<string> Tickers { get; }

        public ObservableCollection<LogEventArg> Logs { get; }
        
        public ObservableCollection<OptionsScreeningResult> OptionsScreeningResults => _filteredOptionsScreeningResults;

        public string SelectedTimeframe
        {
            get { return _selectedTimeframe; }
            set
            {
                if (_selectedTimeframe != value)
                {
                    _selectedTimeframe = value;

                    UpdateFilteredAlerts();
                    UpdateFilteredTrendLines(_selectedTicker);

                    OnPropertyChanged(nameof(SelectedTimeframe));
                }
            }
        }

        public string SelectedTicker
        {
            get { return _selectedTicker; }
            set
            {
                if (_selectedTicker != value)
                {
                    _selectedTicker = value;

                    UpdateFilteredAlerts();
                    UpdateFilteredTrendLines(_selectedTicker);
                    FilterOptionsScreeningResults();

                    OnPropertyChanged(nameof(SelectedTicker));
                }
            }
        }
        
        public string SelectedOptionType
        {
            get { return _selectedOptionType; }
            set
            {
                if (_selectedOptionType != value)
                {
                    _selectedOptionType = value;


                    OnPropertyChanged(nameof(SelectedOptionType));
                }
            }
        }

        public ObservableCollection<Alert> Alerts => _filteredAlerts;
        public ObservableCollection<TrendLine> TrendLines => _filteredTrendLines;

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task GetOptionChain(string ticker, DateTime fromDate, DateTime toDate)
        {
            var optionChain = await _repo.GetOptionChainAsync(ticker, fromDate, toDate);

            if (optionChain == null)
            {
                return;
            }

            _allOptionChain.Clear();

            var expiredOptions = optionChain.Expired ?? Array.Empty<string>();
            var activeOptions = optionChain.Active ?? Array.Empty<string>();
            var allOptions = expiredOptions.Concat(activeOptions).ToList();

            foreach (var option in allOptions)
            {
                _allOptionChain.Add(option);
            }

            FilterOptionChainByType();
            OnPropertyChanged(nameof(AllOptionChain));
        }

        public async Task GetOptionPrice(string optionDetailByBarChart)
        {
            var optionPrice = await _repo.GetOptionPriceAsync(optionDetailByBarChart);

            if (optionPrice == null)
            {
                return;
            }

            _optionPrices.Clear();

            foreach (var option in optionPrice)
            {
                _optionPrices.Add(option);
            }

            OnPropertyChanged(nameof(OptionPrices));
        }
        
        public async Task GetLevels()
        {
            try
            {
                var tickers = TickersWithoutAll;
                var timeframes = new[] { Timeframe.Daily, Timeframe.Hour1};

                foreach (var timeframe in timeframes)
                {
                    _strategy.TrendLineCreated -= Strategy_TrendLineCreated;
                    _strategy.PivotLevelCreated -= Strategy_PivotLevelCreated;
                    _strategy = new SwingPointsLiveTradingHighTimeframesStrategy();
                    
                    _strategy.TrendLineCreated += Strategy_TrendLineCreated;
                    _strategy.PivotLevelCreated += Strategy_PivotLevelCreated;

                    foreach (var ticker in tickers)
                    {
                        IReadOnlyCollection<Price> prices;
                        if (timeframe == Timeframe.Daily)
                        {
                            prices = await _repo.GetStockDataForHighTimeframesAsc(ticker, timeframe, DateTime.Now.AddYears(-10), DateTime.Now.AddDays(1));
                        }
                        else
                        {
                            prices = await _repo.GetStockDataForHighTimeframesAsc(ticker, timeframe, DateTime.Now.AddYears(-5), DateTime.Now.AddDays(1));
                        }
                        
                        _tickerAndPrices[ticker] = prices;
                        UpdateFilteredTrendLines(ticker);
                        
                        var swingPointStrategyParameter = SwingPointParametersProvider.GetSwingPointStrategyParameter(ticker, timeframe);
                        await Task.Run(() =>
                        {
                            _strategy.CheckForTopBottomTouch(ticker, prices.ToList(), swingPointStrategyParameter);
                            return Task.CompletedTask;
                        });
                    }
                }
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

        private void UpdateFilteredAlerts()
        {
            _filteredAlerts.Clear();
            if (SelectedTicker == ALL && SelectedTimeframe.ToString() == ALL)
            {
                foreach (var alert in _allAlerts)
                {
                    _filteredAlerts.Add(alert);
                }
            }
            else
            {
                ObservableCollection<Alert> filtered = null;

                if (SelectedTicker == ALL)
                {
                    filtered = new ObservableCollection<Alert>(_allAlerts.Where(x => x.Timeframe.ToString() == SelectedTimeframe));
                }
                else if (SelectedTimeframe.ToString() == ALL)
                {
                    filtered = new ObservableCollection<Alert>(_allAlerts.Where(x => x.Ticker == SelectedTicker));
                }
                else
                {
                    filtered = new ObservableCollection<Alert>(_allAlerts.Where(x => x.Ticker == SelectedTicker && x.Timeframe.ToString() == SelectedTimeframe));
                }

                foreach (var alert in filtered)
                {
                    _filteredAlerts.Add(alert);
                }
            }
            OnPropertyChanged(nameof(Alerts));
        }
        
        private void UpdateFilteredTrendLines(string ticker)
        {
            if (SelectedTicker == ALL && SelectedTimeframe.ToString() == ALL)
            {
                _filteredTrendLines.Clear();
                foreach (var trendLine in _allTrendLines)
                {
                    _filteredTrendLines.Add(trendLine);
                }
            }
            else
            {
                List<TrendLine> filtered;

                if (SelectedTicker == ALL)
                {
                    filtered = new List<TrendLine>(_allTrendLines.Where(x => x.Timeframe.ToString() == SelectedTimeframe));
                }
                else if (SelectedTimeframe.ToString() == ALL)
                {
                    filtered = new List<TrendLine>(_allTrendLines.Where(x => x.Ticker == SelectedTicker));
                }
                else
                {
                    filtered = new List<TrendLine>(_allTrendLines.Where(x => x.Ticker == SelectedTicker && x.Timeframe.ToString() == SelectedTimeframe));
                }
                
                if (!_tickerAndPrices.ContainsKey(SelectedTicker))
                {
                    return;
                }

                if (ticker != _selectedTicker)
                {
                    return;
                }
                _filteredTrendLines.Clear();
                
                var prices = _tickerAndPrices[SelectedTicker];
                var currentPrice = prices.Last().Close;
                
                // order by level where level is closest to current price
                filtered = filtered
                    .OrderBy(x =>
                    {
                        var endPointValue = (x.End.Close + x.End.Open + x.End.High + x.End.Low) / 4;
                        return Math.Abs(endPointValue - currentPrice);
                    })
                    .ToList();

                foreach (var trendLine in filtered)
                {
                    _filteredTrendLines.Add(trendLine);
                }
            }
            
            OnPropertyChanged(nameof(TrendLines));
        }

        private async Task StartStrategy()
        {
#if DEBUG
            // await RunInDebug();
#else
            await RunInRelease();
#endif
        }

        private async Task RunInDebug()
        {
            var tickers = TickersWithoutAll;
            var timeframes = new[] { Timeframe.Daily };
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
                        
                        var priceToStartTesting = prices.First(x => x.Date >= DateTime.Now.AddMonths(-2));
                        
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
                            
                            await Task.Run(() => {
                                _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
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
            }

            Logs.Add(new LogEventArg($"Finished running strategy at {DateTime.Now}"));
        }

        private void Strategy_PivotLevelCreated(object sender, PivotLevelEventArgs e)
        {
            lock (_lock)
            {
                var pivotLevels = e.PivotLevels;

                foreach (var trendLine in _allTrendLines.ToList())
                {
                    var first = pivotLevels.FirstOrDefault();
                    if (first != null && first.Ticker == trendLine.Ticker && first.Timeframe == trendLine.Timeframe)
                    {
                        _allTrendLines.Remove(trendLine);
                    }
                }
                
                foreach (var pivotLevel in pivotLevels)
                {
                    var trendLine = pivotLevel.ToTrendLine();
                    if (!_allTrendLines.Contains(trendLine))
                    {
                        _allTrendLines.Add(trendLine);
                    }
                }
                
                UpdateFilteredTrendLines(string.Empty);
            }
        }

        private void Strategy_TrendLineCreated(object sender, TrendLineEventArgs e)
        {
            lock (_lock)
            {
                var trendLines = e.TrendLines;

                foreach (var trendLine in _allTrendLines.ToList())
                {
                    var first = trendLines.FirstOrDefault();
                    if (first != null && first.Ticker == trendLine.Ticker && first.Timeframe == trendLine.Timeframe)
                    {
                        _allTrendLines.Remove(trendLine);
                    }
                }
                
                foreach (var trendLine in trendLines)
                {
                    if (!_allTrendLines.Contains(trendLine))
                    {
                        _allTrendLines.Add(trendLine);
                    }
                }
                
                UpdateFilteredTrendLines(string.Empty);
            }
        }

        private async Task RunInRelease()
        {
            while (true)
            {
                var minuteModule = DateTime.Now.Minute % 10;
                if (minuteModule != 0)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5 - minuteModule));
                }
                
                try
                {
                    var tickers = TickersWithoutAll;
                    var timeframes = new[] { Timeframe.Daily };

                    foreach (var timeframe in timeframes)
                    {
                        _strategy.AlertCreated -= Strategy_AlertCreated;
                        _strategy.TrendLineCreated -= Strategy_TrendLineCreated;
                        _strategy.PivotLevelCreated -= Strategy_PivotLevelCreated;
                        
                        _strategy = new SwingPointsLiveTradingHighTimeframesStrategy();
                        
                        _strategy.AlertCreated += Strategy_AlertCreated;
                        _strategy.TrendLineCreated += Strategy_TrendLineCreated;
                        _strategy.PivotLevelCreated += Strategy_PivotLevelCreated;

                        Logs.Add(new LogEventArg($"Started running strategy at {DateTime.Now}"));

                        foreach (var ticker in tickers)
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
                            
                            _tickerAndPrices[ticker] = prices;
                            UpdateFilteredTrendLines(ticker);

                            await Task.Run( async () => {
                                _strategy.CheckForTopBottomTouch(ticker, prices.ToList(), swingPointStrategyParameter);
                                
                                if (timeframe is Timeframe.Minute15 or Timeframe.Minute30)
                                {
                                    var lowTimeframePrices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-3), DateTime.Now);
                                    _strategy.CheckForTouchingDownTrendLine(ticker, lowTimeframePrices.ToList(), swingPointStrategyParameter);
                                    _strategy.CheckForTouchingUpTrendLine(ticker, lowTimeframePrices.ToList(), swingPointStrategyParameter);
                                }
                            });

                        }
                    }
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
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Strategy_AlertCreated(object sender, AlertEventArgs e)
        {
            lock (_lock)
            {
                var alert = e.Alert;
                var existedAlert = _allAlerts
                    .FirstOrDefault(x =>
                        x.Ticker == alert.Ticker && x.Timeframe == alert.Timeframe && x.CreatedAt == alert.CreatedAt);
                
                if (existedAlert != null)
                {
                    _allAlerts.Remove(existedAlert);
                }
                else
                {
                    // in release, we only want to show toast notification for new alerts
#if !DEBUG
                    new ToastContentBuilder()
                    .AddText($"{alert.Ticker} {alert.OrderPosition} {alert.CreatedAt:yyyy-MM-dd HH:mm}")
                    .AddText(alert.Message)
                    .Show();
#endif
                }
                    
                _allAlerts.Add(alert);
                UpdateFilteredAlerts();
            }
        }

        public void ExportAlertToCsv()
        {
            try
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var stockAlertPath = Path.Combine(documents, "StockAlerts", $"{DateTime.Now:yyyyMMddThhmmss}");
                if (!Directory.Exists(stockAlertPath))
                {
                    Directory.CreateDirectory(stockAlertPath);
                }
                var alertCsvByDayPath = Path.Combine(stockAlertPath, "alerts.csv");
                var csv = _allAlerts.Select(a => a.ToCsvString()).ToList();
                var csvString = string.Join(Environment.NewLine, csv);
            
                File.WriteAllText(alertCsvByDayPath, csvString);
                
                //write parameter to json file
                foreach (var ticker in TickersToTrade.POPULAR_TICKERS)
                {
                    var parameter = SwingPointParametersProvider.GetSwingPointStrategyParameter(ticker, Timeframe.Hour1);
                    var parameterJson = parameter.ToJsonString();
                    var parameterPath = Path.Combine(stockAlertPath, $"{ticker}_parameter.json");
                    File.WriteAllText(parameterPath, parameterJson);
                }
                
                new ToastContentBuilder()
                    .AddText("Alerts exported")
                    .AddText($"Alerts exported to {alertCsvByDayPath}")
                    .Show();
            }
            catch (Exception e)
            {
                new ToastContentBuilder()
                    .AddText("Alerts export failed")
                    .AddText(e.Message)
                    .Show();
            }
        }

        public void FilterOptionChainByType()
        {
            _filteredOptionChain.Clear();
            var isValidOptionType = SelectedOptionType.Equals("P", StringComparison.CurrentCultureIgnoreCase) || SelectedOptionType.Equals("C", StringComparison.CurrentCultureIgnoreCase);
            
            if (isValidOptionType)
            {
                foreach (var optionChain in _allOptionChain)
                {
                    if (optionChain.EndsWith(SelectedOptionType, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _filteredOptionChain.Add(optionChain);
                    }
                }
            }
            else
            {
                foreach (var optionChain in _allOptionChain)
                {
                    _filteredOptionChain.Add(optionChain);
                }
            }
            
            OnPropertyChanged(nameof(AllOptionChain));
        }

        public async Task ScreenOptions(OptionsScreeningParams screeningParams)
        {
            await GetScreenedOptions(screeningParams);
            FilterOptionsScreeningResults();
        }
        
        private void FilterOptionsScreeningResults()
        {
            _filteredOptionsScreeningResults.Clear();
            var optionsScreeningResults = _allOptionsScreeningResults.ToList();
            
            if (_selectedTicker == ALL)
            {
                foreach (var result in optionsScreeningResults)
                {
                    _filteredOptionsScreeningResults.Add(result);
                }
            }
            else
            {
                foreach (var result in optionsScreeningResults)
                {
                    if (result.UnderlyingSymbol == _selectedTicker)
                    {
                        _filteredOptionsScreeningResults.Add(result);
                    }
                }
            }
        }
        
        private async Task GetScreenedOptions(OptionsScreeningParams screeningParams)
        {
            var optionsScreeningResults = await _repo.GetOptionsScreeningResults(screeningParams);
            _allOptionsScreeningResults.Clear();
            
            foreach (var result in optionsScreeningResults)
            {
                _allOptionsScreeningResults.Add(result);
            }
        }
    }
}