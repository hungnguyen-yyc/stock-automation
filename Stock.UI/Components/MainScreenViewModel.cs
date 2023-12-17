using Stock.Data;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;
using Syncfusion.Windows.Controls.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Stock.UI.Components
{
    public class MainScreenViewModel : INotifyPropertyChanged
    {
        private readonly StockDataRepository _repo;
        private readonly SwingPointsLiveTradingStrategy _strategy;
        private IReadOnlyCollection<Price> _prices;
        private Timeframe _selectedTimeframe;
        private string _selectedTicker;
        private ObservableCollection<Alert> _alerts;
        private readonly object _lock = new();

        public MainScreenViewModel(StockDataRepository repo, SwingPointsLiveTradingStrategy strategy)
        {
            _repo = repo;
            _strategy = strategy;
            _prices = new List<Price>();
            _selectedTimeframe = Timeframe.Daily;
            _selectedTicker = "TSLA";
            _alerts = new ObservableCollection<Alert>();
            BindingOperations.EnableCollectionSynchronization(_alerts, _lock);

            Tickers = new ObservableCollection<string>
            {
                "All"
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
                    OnPropertyChanged(nameof(SelectedTicker));
                }
            }
        }

        public ObservableCollection<Alert> Alerts => _alerts;

        // Implement INotifyPropertyChanged to notify the View of property changes
        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task StartStrategy()
        {
            while (true)
            {
                var tickerBatch = new[] { TickersToTrade.POPULAR_TICKERS };
#if DEBUG
                var timeframes = new[] { Timeframe.Minute15 };
                var numberOfCandlesticksToLookBacks = new[] { 14 };
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
                        await Parallel.ForEachAsync(numberOfCandlesticksToLookBacks, parallelOptions, async (numberOfCandlestickToLookBack, token) =>
                        {
                            var swingPointStrategyParameter = new SwingPointStrategyParameter
                            {
                                NumberOfSwingPointsToLookBack = 6,
                                NumberOfCandlesticksToLookBack = numberOfCandlestickToLookBack,
                                NumberOfCandlesticksToSkipAfterSwingPoint = 2
                            };

                            await Parallel.ForEachAsync(tickers, parallelOptions, async (ticker, token) =>
                            {
                                var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-3), DateTime.Now);
                                for (int i = 200; i < prices.Count; i++)
                                {
                                    _strategy.CheckForBreakBelowUpTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                                    //_strategy.CheckForBreakAboveDownTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                                }
                            });
                        });
                    });
                });
                await Task.Delay(TimeSpan.FromMinutes(15));
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
                if (!Alerts.Contains(alert))
                {
                    Alerts.Add(alert);
                    OnPropertyChanged(nameof(Alerts));
                }
            }
        }
    }
}
