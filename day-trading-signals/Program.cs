using day_trading_signals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureHostConfiguration(h => { })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<MyProcess>();
            }).UseConsoleLifetime().Build();
        host.Run();
    }
}