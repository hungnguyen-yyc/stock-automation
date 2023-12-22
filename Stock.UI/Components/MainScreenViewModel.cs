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
        private string _selectedTimeframe;
        private string _selectedTicker;
        private ObservableCollection<Alert> _allAlerts;
        private ObservableCollection<Alert> _filteredAlerts;
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

        public ObservableCollection<string> Timeframes { get; }

        public ObservableCollection<string> Tickers { get; }

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

        // Implement INotifyPropertyChanged to notify the View of property changes
        public event PropertyChangedEventHandler? PropertyChanged;

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
                var tickerBatch = new[] { TickersToTrade.POPULAR_TICKERS };
#if DEBUG
                var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1, Timeframe.Daily };
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
                            var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker, timeframe);

                            var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-6), DateTime.Now);

#if DEBUG
                            for (int i = 1000; i < prices.Count; i++)
                            {
                                var task3 = Task.Run(() =>
                                {
                                    _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                                });

                                await Task.WhenAll(task3);
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
                Debug.WriteLine($"Finished running strategy at {DateTime.Now}");
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

        private async Task RunInRelease()
        {
            while (true)
            {
                var tickerBatch = new[] { TickersToTrade.POPULAR_TICKERS };
                var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1, Timeframe.Daily };

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
                            var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker, timeframe);

                            var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-3), DateTime.Now);

#if DEBUG
                            for (int i = 3141; i < prices.Count; i++)
                            {
                                var task3 = Task.Run(() =>
                                {
                                    _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                                });

                                await Task.WhenAll(task3);
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
                Debug.WriteLine($"Finished running strategy at {DateTime.Now}");
            }
        }

        private SwingPointStrategyParameter GetSwingPointStrategyParameter(string ticker, Timeframe timeframe)
        {
            switch (ticker)
            {
                case "TSLA":
                    return new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 10,
                        NumberOfCandlesticksToLookBack = 30,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 2,
                        NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390, // 390 candles means 15 days in 15 minute timeframe,
                        Timeframe = timeframe,
                        NumberOfCandlesticksBeforeCurrentPriceToLookBack = 5,
                        NumberOfCandlesticksIntersectForTopsAndBottoms = 5,
                    };
                case "AMD":
                    return new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 7,
                        NumberOfCandlesticksToLookBack = 14,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 2,
                        NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
                        Timeframe = timeframe,
                        NumberOfCandlesticksBeforeCurrentPriceToLookBack = 5,
                        NumberOfCandlesticksIntersectForTopsAndBottoms = 3,
                    };
                default:
                    return new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 7,
                        NumberOfCandlesticksToLookBack = 14,
                        NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                        NumberOfTouchesToDrawTrendLine = 2,
                        NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
                        Timeframe = timeframe,
                        NumberOfCandlesticksBeforeCurrentPriceToLookBack = 5,
                        NumberOfCandlesticksIntersectForTopsAndBottoms = 2,
                    };
            }
        }
    }
}