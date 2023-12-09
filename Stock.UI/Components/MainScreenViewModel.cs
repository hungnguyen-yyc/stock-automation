using Stock.Data;
using Stock.Shared.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Stock.UI.Components
{
    public class MainScreenViewModel : INotifyPropertyChanged
    {
        private readonly StockDataRepository _repo;
        private IReadOnlyCollection<Price> _prices;
        private Timeframe _selectedTimeframe;
        private string _selectedTicker;

        public MainScreenViewModel(StockDataRepository repo)
        {
            _repo = repo;
            _prices = new List<Price>();
            _selectedTimeframe = Timeframe.Daily;
            _selectedTicker = "TSLA";
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

        public ObservableCollection<string> Tickers => new ObservableCollection<string> { "TSLA", "AAPL", "SPY" };

        public Timeframe SelectedTimeframe
        {
            get { return _selectedTimeframe; }
            set
            {
                if (_selectedTimeframe != value)
                {
                    _selectedTimeframe = value;
                    OnPropertyChanged(nameof(SelectedTimeframe));
                    LoadPrices();
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
                    LoadPrices();
                }
            }
        }

        public async Task LoadPrices()
        {
            var prices = await _repo.GetStockData(SelectedTicker, SelectedTimeframe, DateTime.Now.AddMonths(-2), DateTime.Now);

            if (prices == null)
            {
                return;
            }

            Prices = prices.ToArray();
        }

        // Implement INotifyPropertyChanged to notify the View of property changes
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
