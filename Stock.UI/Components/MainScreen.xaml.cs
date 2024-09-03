using Stock.Shared.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Stock.Shared.Models.Parameters;

namespace Stock.UI.Components
{
    /// <summary>
    /// Interaction logic for MainScreen.xaml
    /// </summary>
    public partial class MainScreen : Window
    {
        private MainScreenViewModel viewModel;

        public MainScreen()
        {
            InitializeComponent();

            viewModel = new MainScreenViewModel(new Data.StockDataRepository(), new Strategies.SwingPointsLiveTradingLowTimeframesStrategy());
            DataContext = viewModel;
        }

        private void lsvAlertsColumnHeader_Click(object sender, RoutedEventArgs e)
		{
            GridViewColumnHeader column = e.OriginalSource as GridViewColumnHeader;

            if (column != null)
            {
                string propertyName = column.Tag as string;

                if (!string.IsNullOrEmpty(propertyName))
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(lsvAlerts.ItemsSource);

                    ListSortDirection direction = ListSortDirection.Ascending;

                    if (view.SortDescriptions.Count > 0 && view.SortDescriptions[0].PropertyName == propertyName)
                    {
                        direction = (view.SortDescriptions[0].Direction == ListSortDirection.Ascending)
                            ? ListSortDirection.Descending
                            : ListSortDirection.Ascending;
                    }

                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription(propertyName, direction));
                }
            }
        }
        
        private void lsvLogsColumnHeader_Click(object sender, RoutedEventArgs e)
		{
            GridViewColumnHeader column = e.OriginalSource as GridViewColumnHeader;

            if (column != null)
            {
                string propertyName = column.Tag as string;

                if (!string.IsNullOrEmpty(propertyName))
                {
                    ICollectionView view = CollectionViewSource.GetDefaultView(lsvLogs.ItemsSource);

                    ListSortDirection direction = ListSortDirection.Ascending;

                    if (view.SortDescriptions.Count > 0 && view.SortDescriptions[0].PropertyName == propertyName)
                    {
                        direction = (view.SortDescriptions[0].Direction == ListSortDirection.Ascending)
                            ? ListSortDirection.Descending
                            : ListSortDirection.Ascending;
                    }

                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription(propertyName, direction));
                }
            }
        }
        
        private void lsvTrendLinesColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var column = e.OriginalSource as GridViewColumnHeader;

            if (column != null)
            {
                var propertyName = column.Tag as string;

                if (!string.IsNullOrEmpty(propertyName))
                {
                    var view = CollectionViewSource.GetDefaultView(lsvTrendLines.ItemsSource);

                    var direction = ListSortDirection.Ascending;

                    if (view.SortDescriptions.Count > 0 && view.SortDescriptions[0].PropertyName == propertyName)
                    {
                        direction = (view.SortDescriptions[0].Direction == ListSortDirection.Ascending)
                            ? ListSortDirection.Descending
                            : ListSortDirection.Ascending;
                    }

                    view.SortDescriptions.Clear();
                    view.SortDescriptions.Add(new SortDescription(propertyName, direction));
                }
            }
        }

        private void lsvAlerts_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var alert = ((FrameworkElement)e.OriginalSource).DataContext as Alert;
            if (alert != null)
            {
                var ticker = alert.Ticker;
                var date = alert.CreatedAt;
                var fridayNextWeek = date.AddDays((int)DayOfWeek.Friday - (int)date.DayOfWeek + 7);
                viewModel.GetOptionChain(ticker, date, fridayNextWeek);
            }
        }

        private void lsvOptionChain_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var alert = ((FrameworkElement)e.OriginalSource).DataContext as string;
            if (alert != null)
            {
                viewModel.GetOptionPrice(alert);
            }
        }
        
        private void BtnExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ExportAlertToCsv();
        }

        private void btnGetLevels_Click(object sender, RoutedEventArgs e)
        {
            viewModel.GetLevels();
        }

        private void BtnAddAndSearch_Click(object sender, RoutedEventArgs e)
        {
            var newTicker = txtTicker.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(newTicker))
            {
                return;
            }
            
            if (viewModel.Tickers.Contains(newTicker, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }
            
            viewModel.Tickers.Insert(1, newTicker); // index 0 is for "All"
            txtTicker.Text = string.Empty;
            viewModel.GetLevels();
        }

        private void lsvScreenedOptions_Click(object sender, RoutedEventArgs e)
        {
            var column = e.OriginalSource as GridViewColumnHeader;

            if (column == null) return;
            
            var propertyName = column.Tag as string;

            if (string.IsNullOrEmpty(propertyName)) return;
            
            var view = CollectionViewSource.GetDefaultView(lsvScreenedOptions.ItemsSource);

            var direction = ListSortDirection.Ascending;

            var sortDescriptions = view.SortDescriptions.ToList();
            view.SortDescriptions.Clear();
            
            var sortedField = sortDescriptions.FirstOrDefault(x => x.PropertyName == propertyName);

            // this is because the FirstOrDefault of SortDescriptions is not returning null when not found or empty
            if (sortedField.PropertyName == propertyName)
            {
                var oldDirection = sortedField.Direction;
                sortDescriptions.Remove(sortedField);
                direction = oldDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                sortDescriptions.Add(new SortDescription(propertyName, direction));
            }
            else
            {
                sortDescriptions.Add(new SortDescription(propertyName, direction));
            }

            foreach (var sortDescription in sortDescriptions)
            {
                view.SortDescriptions.Add(sortDescription);
            }
            view.Refresh();
        }

        private void btnScreeningOptionsClearSorting_Click(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(lsvScreenedOptions.ItemsSource);
            view.SortDescriptions.Clear();
        }

        private void btnScreeningOptions_Click(object sender, RoutedEventArgs e)
        {
            var screeningParams = GetOptionsScreeningParams();
            viewModel.ScreenOptions(screeningParams);
        }

        private OptionsScreeningParams GetOptionsScreeningParams()
        {
            var screeningParams = OptionsScreeningParams.Default;
            if (txtMinVolume.Text.Trim().Length > 0 && int.TryParse(txtMinVolume.Text, out var minVolume))
            {
                screeningParams.MinVolume = minVolume;
            }
            if (txtMaxVolume.Text.Trim().Length > 0 && int.TryParse(txtMaxVolume.Text, out var maxVolume))
            {
                screeningParams.MaxVolume = maxVolume;
            }
            if (txtMinOpenInterest.Text.Trim().Length > 0 && int.TryParse(txtMinOpenInterest.Text, out var minOpenInterest))
            {
                screeningParams.MinOpenInterest = minOpenInterest;
            }
            if (txtMaxOpenInterest.Text.Trim().Length > 0 && int.TryParse(txtMaxOpenInterest.Text, out var maxOpenInterest))
            {
                screeningParams.MaxOpenInterest = maxOpenInterest;
            }
            if (txtMinDTE.Text.Trim().Length > 0 && int.TryParse(txtMinDTE.Text, out var minDte))
            {
                screeningParams.MinExpirationDays = minDte;
            }
            if (txtMaxDTE.Text.Trim().Length > 0 && int.TryParse(txtMaxDTE.Text, out var maxDte))
            {
                screeningParams.MaxExpirationDays = maxDte;
            }
            
            return screeningParams;
        }

        private void lsvScreenedOptions_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is not OptionsScreeningResult option) return;
            
            var optionType = option.Type.Contains("call", StringComparison.InvariantCultureIgnoreCase) ? OptionTypeEnum.C : OptionTypeEnum.P;
            var optionString = $"{option.UnderlyingSymbol}|{option.ExpirationDate:yyyyMMdd}|{option.Strike}{optionType}";
            viewModel.GetOptionPrice(optionString);
        }
    }
}
