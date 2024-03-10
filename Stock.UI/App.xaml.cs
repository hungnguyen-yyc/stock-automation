using Syncfusion.Licensing;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Stock.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF1cWWhIfEx1RHxQdld5ZFRHallYTnNWUj0eQnxTdEZiWH1ccHRXT2NeWEd2XQ==");

            InitializeComponent();

            Application.Current.Exit += Application_Exiting;
        }

        private void Application_Exiting(object sender, ExitEventArgs e)
        {
            //viewModel.Stop();
        }
    }

}
