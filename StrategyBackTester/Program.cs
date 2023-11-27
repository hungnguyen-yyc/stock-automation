using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skender.Stock.Indicators;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace StrategyBackTester
{
    internal class Program
    {
        static void Main(string[] args)
        {
#if (!DEBUG)
            var host = new HostBuilder()
                .ConfigureHostConfiguration(h => { })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService(services => new SwingPointBackTestRunner());
                }).UseConsoleLifetime().Build();
            host.Run();
#else
            var swingbacktest = new SwingPointBackTestRunner();
            swingbacktest.Run();
#endif
        }
    }
}