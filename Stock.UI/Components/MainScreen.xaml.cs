﻿using Stock.Shared.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Stock.UI.Components
{
    /// <summary>
    /// Interaction logic for MainScreen.xaml
    /// </summary>
    public partial class MainScreen : Page
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

        private void CalOptionDate_DateSelected(object? sender, SelectionChangedEventArgs e)
        {
            var date = this.CalOptionDate.SelectedDate;
            
            if (date != null)
            {
                var fridayNextWeek = date.Value
                    .AddDays((int)DayOfWeek.Friday - (int)date.Value.DayOfWeek + 7);
                viewModel.GetOptionChain(viewModel.SelectedTicker, date.Value, fridayNextWeek);
            }
        }

        private void btnGetLevels_Click(object sender, RoutedEventArgs e)
        {
            viewModel.GetLevels();
        }

        private void OptionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel?.FilterOptionChainByType();
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
    }
}
