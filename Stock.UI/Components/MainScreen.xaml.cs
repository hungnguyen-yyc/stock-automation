using Stock.DataProvider;
using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        public MainScreen()
        {
            InitializeComponent();

            var viewModel = new MainScreenViewModel(new Data.StockDataRepository(), new Strategies.SwingPointsLiveTradingStrategy());
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
    }
}
