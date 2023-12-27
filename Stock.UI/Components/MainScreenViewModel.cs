using Stock.Data;
using Stock.Data.EventArgs;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;

namespace Stock.UI.Components
{
    public class MainScreenViewModel : INotifyPropertyChanged
    {
        private const string ALL = "All";

        private readonly StockDataRepository _repo;
        private readonly SwingPointsLiveTradingStrategy _strategy;
        private IReadOnlyCollection<Price> _prices;
        private string _selectedTimeframe;
        private string _selectedTicker;
        private ObservableCollection<Alert> _allAlerts;
        private ObservableCollection<Alert> _filteredAlerts;
        private ObservableCollection<string> _optionChain;
        private ObservableCollection<string> _optionPrice;
        private readonly object _lock = new();

        public MainScreenViewModel(StockDataRepository repo, SwingPointsLiveTradingStrategy strategy)
        {
            _repo = repo;
            _strategy = strategy;
            _prices = new List<Price>();
            _selectedTimeframe = Timeframe.Minute15.ToString();
            _selectedTicker = ALL;
            _allAlerts = new ObservableCollection<Alert>();
            _filteredAlerts = new ObservableCollection<Alert>();
            BindingOperations.EnableCollectionSynchronization(_filteredAlerts, _lock);

            Logs = new ObservableCollection<LogEventArg>();
            BindingOperations.EnableCollectionSynchronization(Logs, _lock);

            _optionChain = new ObservableCollection<string>();
            _optionPrice = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(_optionChain, _lock);
            BindingOperations.EnableCollectionSynchronization(_optionPrice, _lock);

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

            _strategy.AlertCreated += Strategy_AlertCreated;
            StartStrategy();
        }

        public IReadOnlyCollection<Price> Prices
        {
            get => _prices;
            set
            {
                _prices = value;
                OnPropertyChanged(nameof(Prices));
            }
        }

        public ObservableCollection<string> OptionChain => _optionChain;

        public ObservableCollection<string> OptionPrice => _optionPrice;

        public ObservableCollection<string> Timeframes { get; }

        public ObservableCollection<string> Tickers { get; }

        public ObservableCollection<LogEventArg> Logs { get; }

        public string SelectedTimeframe
        {
            get { return _selectedTimeframe; }
            set
            {
                if (_selectedTimeframe != value)
                {
                    _selectedTimeframe = value;

                    UpdateFilteredAlerts();

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

                    OnPropertyChanged(nameof(SelectedTicker));
                }
            }
        }

        public ObservableCollection<Alert> Alerts => _filteredAlerts;

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task GetOptionChain(string ticker, DateTime fromDate, DateTime toDate)
        {
            var optionChain = await _repo.GetOptionsAsync(ticker, fromDate, toDate);

            if (optionChain == null)
            {
                return;
            }

            _optionChain.Clear();

            var expiredOptions = optionChain.Expired ?? Array.Empty<string>();
            var activeOptions = optionChain.Active ?? Array.Empty<string>();
            var allOptions = expiredOptions.Concat(activeOptions).ToList();

            foreach (var option in allOptions)
            {
                _optionChain.Add(option);
            }

            OnPropertyChanged(nameof(OptionChain));
        }

        public async Task GetOptionPrice(string optionDetailByBarChart)
        {
            var optionPrice = await _repo.GetOptionPriceAsync(optionDetailByBarChart);

            if (optionPrice == null)
            {
                return;
            }

            _optionPrice.Clear();

            foreach (var option in optionPrice)
            {
                _optionPrice.Add(option);
            }

            OnPropertyChanged(nameof(OptionPrice));
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

        private async Task StartStrategy()
        {
#if DEBUG
            await RunInDebug();
#else
            await RunInRelease();
#endif
        }

        private async Task RunInDebug()
        {
            while (true)
            {
                var tickers = TickersToTrade.POPULAR_TICKERS;
                var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1, Timeframe.Daily };

                foreach (var timeframe in timeframes)
                {
                    foreach (var ticker in tickers)
                    {
                        await _repo.FillLatestDataForTheDay(ticker, timeframe, DateTime.Now, DateTime.Now);
                        var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker, timeframe);

                        var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-12), DateTime.Now);

                        for (int i = 3000; i < prices.Count; i++)
                        {
                            await Task.Run(() => _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter));
                        }
                    }
                }
                Debug.WriteLine($"Finished running strategy at {DateTime.Now}");
            }
        }
        private async Task RunInRelease()
        {
            while (true)
            {
                try
                {
                    Logs.Add(new LogEventArg($"Started running strategy at {DateTime.Now}"));

                    var tickers = TickersToTrade.POPULAR_TICKERS;
                    var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1, Timeframe.Daily };

                    foreach (var timeframe in timeframes)
                    {
                        foreach (var ticker in tickers)
                        {
                            await _repo.FillLatestDataForTheDay(ticker, timeframe, DateTime.Now, DateTime.Now);
                            var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker, timeframe);

                            var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-6), DateTime.Now);

                            await Task.Run(() => _strategy.CheckForTopBottomTouch(ticker, prices.ToList(), swingPointStrategyParameter));
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(15.0));
                }
                catch (Exception ex)
                {
                    Logs.Add(new LogEventArg(ex.Message));
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
                if (!_allAlerts.Contains(alert))
                {
                    _allAlerts.Add(alert);
                    UpdateFilteredAlerts();
                }
            }
        }

        private SwingPointStrategyParameter GetSwingPointStrategyParameter(string ticker, Timeframe timeframe)
        {
            switch (ticker)
            {
                default:
                    return new SwingPointStrategyParameter
                    {
                        NumberOfCandlesticksToLookBack = 21,
                        Timeframe = timeframe,
                        NumberOfCandlesticksIntersectForTopsAndBottoms = 10,

                        NumberOfSwingPointsToLookBack = 7,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 2,
                        NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
                        NumberOfCandlesticksBeforeCurrentPriceToLookBack = 7,
                    };
            }
        }
    }
}