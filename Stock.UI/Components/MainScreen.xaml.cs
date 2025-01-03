﻿using Stock.Shared.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Stock.Shared.Models.Parameters;
using Stock.Strategies;

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

            viewModel = new MainScreenViewModel(new Data.StockDataRetrievalService(), new Strategies.SwingPointsLiveTradingHighTimeframesStrategy());
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
            if (((FrameworkElement)e.OriginalSource).DataContext is Alert alert)
            {
                viewModel.GetSelectedTickerOptionFlowOverview(alert.Ticker, HighChangeInOpenInterestStrategy.OptionsScreeningParams);
            }
            if (((FrameworkElement)e.OriginalSource).DataContext is HighChangeInOpenInterestStrategyAlert highChangeInOpenInterestStrategyAlert)
            {
                var optionTicker = highChangeInOpenInterestStrategyAlert.OptionTicker;
                viewModel.GetOptionPrice(optionTicker);
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
            viewModel.ScreenOptions(txtFilterScreeningByTicker.Text);
        }

        private void lsvScreenedOptions_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is not OptionsScreeningResult option) return;
            
            var optionType = option.Type.Contains("call", StringComparison.InvariantCultureIgnoreCase) ? OptionTypeEnum.C : OptionTypeEnum.P;
            var optionString = $"{option.UnderlyingSymbol}|{option.ExpirationDate:yyyyMMdd}|{option.Strike}{optionType}";
            viewModel.GetOptionPrice(optionString);
            viewModel.GetSelectedTickerOptionFlowOverview(option.UnderlyingSymbol, viewModel.ScreeningParams);
        }

        private void txtFilterScreeningByTicker_TextChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.FilterOptionsScreeningResults(txtFilterScreeningByTicker.Text);
        }

        private void btnQuickOptionSearch_Click(object sender, RoutedEventArgs e)
        {
            var option = $"{txtQuickOptionSearchTicker.Text}|{txtQuickOptionSearchExpiry.Text}|{txtQuickOptionSearchStrike.Text}{txtQuickOptionSearchPutCall.Text}";
            viewModel.GetOptionPrice(option.ToUpper());
        }

        private void btnClearQuickOptionSearch_Click(object sender, RoutedEventArgs e)
        {
            txtQuickOptionSearchExpiry.Text = String.Empty;
            txtQuickOptionSearchTicker.Text = String.Empty;
            txtQuickOptionSearchStrike.Text = String.Empty;
            txtQuickOptionSearchPutCall.Text = String.Empty;
        }
    }
}
