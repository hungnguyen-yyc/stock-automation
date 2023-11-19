using Microsoft.Extensions.Hosting;

namespace day_trading_signals
{
    internal class MyProcess : BackgroundService
    {
        private readonly int[] _interval;

        public MyProcess(params int[] interval)
        {
            _interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var favs = new List<string>() { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "MARA", "RIOT", "RBLX", "SPY", "QQQ", "CAT", "DIS" };

            foreach (var interval in _interval)
            {
                RunEachInterval(favs, interval, stoppingToken);
            }
            
            return;
        }

        private async Task RunEachInterval(List<string> favs, int interval, CancellationToken stoppingToken)
        {
            // create easter time zone
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
            var marketOpen = new DateTime(now.Year, now.Month, now.Day, 9, 30, 0);
            var marketClose = new DateTime(now.Year, now.Month, now.Day, 16, 0, 0);

            while (!stoppingToken.IsCancellationRequested && now < marketClose)
            {
                // run task every interval minutes from market open to market close
                if (now > marketOpen && now < marketClose)
                {
                    Console.WriteLine($"Running at {now}");
                    await KaufmanMfiRunner.Run(favs, interval);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
            }
            Environment.Exit(0);
        }
    }
}
