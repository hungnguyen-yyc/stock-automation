using Stock.DataProvider;
using Stock.Shared.Helpers;
using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.UI.Components
{
    public class MainScreenViewModel : INotifyPropertyChanged
    {
        private readonly FmpStockDataProvider _fmpStockDataProvider;
        private IReadOnlyCollection<Price> _prices;
        private Timeframe _selectedTimeframe;
        private string _selectedTicker;

        public MainScreenViewModel(FmpStockDataProvider fmpStockDataProvider)
        {
            this._fmpStockDataProvider = fmpStockDataProvider;
            _prices = new List<Price>();
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

        public ObservableCollection<string> Tickers => new ObservableCollection<string> { "TSLA", "AAPL", "MSFT", "GOOG", "AMZN" };

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
            var prices = await _fmpStockDataProvider.CollectData(SelectedTicker, SelectedTimeframe, DateTime.Now.AddYears(-5));

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
