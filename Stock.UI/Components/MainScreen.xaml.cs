using Stock.DataProvider;
using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

            viewModel = new MainScreenViewModel(new Data.StockDataRepository(), new Strategies.SwingPointsLiveTrading15MinStrategy());
            viewModel.IBKRConnected += OnIBKRConnected;
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

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Connect();
        }

        private void OnIBKRConnected(bool isConnected)
        {
            if (isConnected)
            {
                btnConnect.Content = "Disconnect";
            }
            else
            {
                btnConnect.Content = "Connect";
            }
        }
    }
}
