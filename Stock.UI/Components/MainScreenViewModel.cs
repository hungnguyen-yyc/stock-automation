using Stock.Data;
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
        private Timeframe _selectedTimeframe;
        private string _selectedTicker;
        private ObservableCollection<Alert> _allAlerts;
        private ObservableCollection<Alert> _filteredAlerts;
        private readonly object _lock = new();

        public MainScreenViewModel(StockDataRepository repo, SwingPointsLiveTradingStrategy strategy)
        {
            _repo = repo;
            _strategy = strategy;
            _prices = new List<Price>();
            _selectedTimeframe = Timeframe.Daily;
            _selectedTicker = ALL;
            _allAlerts = new ObservableCollection<Alert>();
            _filteredAlerts = new ObservableCollection<Alert>();
            BindingOperations.EnableCollectionSynchronization(_filteredAlerts, _lock);

            Tickers = new ObservableCollection<string>
            {
                ALL
            };

            foreach (var ticker in TickersToTrade.POPULAR_TICKERS)
            {
                Tickers.Add(ticker);
            }

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

        public ObservableCollection<Timeframe> Timeframes => new ObservableCollection<Timeframe>(Enum.GetValues<Timeframe>());

        public ObservableCollection<string> Tickers { get; }

        public Timeframe SelectedTimeframe
        {
            get { return _selectedTimeframe; }
            set
            {
                if (_selectedTimeframe != value)
                {
                    _selectedTimeframe = value;
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

        // Implement INotifyPropertyChanged to notify the View of property changes
        public event PropertyChangedEventHandler? PropertyChanged;

        private void UpdateFilteredAlerts()
        {
            _filteredAlerts.Clear();
            if (SelectedTicker == ALL)
            {
                foreach (var alert in _allAlerts)
                {
                    _filteredAlerts.Add(alert);
                }
            }
            else
            {
                foreach (var alert in _allAlerts)
                {
                    if (alert.Ticker == SelectedTicker)
                    {
                        _filteredAlerts.Add(alert);
                    }
                }
            }
            OnPropertyChanged(nameof(Alerts));
        }

        private async Task StartStrategy()
        {
            while (true)
            {
                var tickerBatch = new[] { TickersToTrade.POPULAR_TICKERS };
#if DEBUG
                var timeframes = new[] { Timeframe.Minute15 };
#else
                var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1, Timeframe.Daily };
                var numberOfCandlesticksToLookBacks = new[] {  15, 30  };
#endif

                var dateAtRun = DateTime.Now.ToString("yyyy-MM-dd");
                var timeAtRun = DateTime.Now.ToString("HH-mm");
                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                await Parallel.ForEachAsync(tickerBatch, parallelOptions, async (tickers, token) =>
                {
                    await Parallel.ForEachAsync(timeframes, parallelOptions, async (timeframe, token) =>
                    {
                        await Parallel.ForEachAsync(tickers, parallelOptions, async (ticker, token) =>
                        {
                            var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker);

                            var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-3), DateTime.Now);

#if DEBUG
                            for (int i = 200; i < prices.Count; i++)
                            {
                                var task = Task.Run(() =>
                                {
                                    _strategy.CheckForBreakBelowUpTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                                });

                                var task2 = Task.Run(() =>
                                {
                                    _strategy.CheckForBreakAboveDownTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                                });

                                await Task.WhenAll(task, task2);
                            }
#else
                            for (int i = 200; i < prices.Count; i++)
                            {
                                var task = Task.Run(() =>
                                {
                                    _strategy.CheckForBreakBelowUpTrendLine(ticker, prices.ToList(), swingPointStrategyParameter);
                                });

                                var task2 = Task.Run(() =>
                                {
                                    _strategy.CheckForBreakAboveDownTrendLine(ticker, prices.ToList(), swingPointStrategyParameter);
                                });

                                await Task.WhenAll(task, task2);
                            }
#endif
                        });
                    });
                });
                await Task.Delay(TimeSpan.FromMinutes(60));
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

        private SwingPointStrategyParameter GetSwingPointStrategyParameter(string ticker)
        {
            switch (ticker)
            {
                case "TSLA":
                    return new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 6,
                        NumberOfCandlesticksToLookBack = 14,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 3,
                    };
                case "SPY":
                    return new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 6,
                        NumberOfCandlesticksToLookBack = 21,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 3,
                    };
                default:
                    return new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 6,
                        NumberOfCandlesticksToLookBack = 21,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 3,
                    };
            }
        }
    }
}