using Syncfusion.Licensing;
using System.Configuration;
using System.Data;
using System.Diagnostics;
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
            try
            {
                InitializeComponent(); 
                Current.Exit += Application_Exiting;
            }
            catch (Exception ex)
            {
                var pathToDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var pathToLogFile = System.IO.Path.Combine(pathToDocuments, $"SSS-{DateTime.Now:yyyy-MM-ddTHH:mm:ss}.txt");
                System.IO.File.WriteAllText(pathToLogFile, ex.ToString());
            }
        }

        private void Application_Exiting(object sender, ExitEventArgs e)
        {
            Debug.WriteLine("You are exiting the application.");
        }
    }

}
