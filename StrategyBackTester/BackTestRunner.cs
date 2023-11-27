using Stock.Shared.Models;
using Stock.Strategies;

namespace StrategyBackTester
{
    internal class BackTestRunner
    {
        public void Run()
        {
            var tickers = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "MARA", "RIOT", "RBLX", "SPY", "QQQ", "CAT", "DIS" };
            var timeFrame = Timeframe.Daily;

            foreach (var ticker in tickers)
            {
                var strategy = new SwingPointsStrategy();
                var orders = strategy.Run(ticker, null, DateTime.Now.AddYears(-3), timeFrame);

                if (orders == null || orders.Count < 2)
                {
                    continue;
                }

                var strategyName = $"C:/Users/hnguyen/Documents/stock-back-test/strategies/{nameof(SwingPointsStrategy)}/{timeFrame}";
                var fileNameWithoutExtension = $"{orders.FirstOrDefault()?.Ticker}-{DateTime.Now:yyyyMMdd-hhmmss}";
                var fileName = $"{fileNameWithoutExtension}.txt";

                if (!Directory.Exists(strategyName))
                {
                    Directory.CreateDirectory(strategyName);
                }

                var filePath = Path.Combine(strategyName, fileName);
                var filePathResult = Path.Combine(strategyName, $"{fileNameWithoutExtension}-result.txt");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                var totalOfOrders = orders.Count;
                var wins = 0;
                var losses = 0;
                var positionDays = new List<int>();
                for (int i = 1; i < orders.Count; i++)
                {
                    var previousOrder = orders[i - 1];
                    var currentOrder = orders[i];
                    
                    if (i % 2 == 0)
                    {
                        continue;
                    }

                    File.AppendAllText(filePath, previousOrder.ToString());
                    File.AppendAllText(filePath, "\n");
                    File.AppendAllText(filePath, currentOrder.ToString());
                    if (previousOrder.Type == OrderType.Long)
                    {
                        if (currentOrder.Price.Close > previousOrder.Price.Close)
                        {
                            wins++;
                        }
                        else
                        {
                            losses++;
                            File.AppendAllText(filePath, " (L)");
                        }
                    }
                    else
                    {
                        if (currentOrder.Price.Close < previousOrder.Price.Close)
                        {
                            wins++;
                        }
                        else
                        {
                            losses++;
                            File.AppendAllText(filePath, " (L)");
                        }
                    }

                    File.AppendAllText(filePath, "\n");
                    positionDays.Add((currentOrder.Time - previousOrder.Time).Days);
                }

                File.AppendAllLines(filePathResult, new List<string> {
                    $"Strategy: {nameof(SwingPointsStrategy)}",
                    $"Timeframe: {timeFrame}",
                    $"Ticker: {ticker}",
                    $"Description: {strategy.Description}",
                    $"Total of orders: {totalOfOrders}", 
                    $"Total of wins: {wins}", 
                    $"Total of losses: {losses}",
                    $"Win rate: {((decimal)wins / (decimal)(wins + losses)) * 100:F}%",
                    $"Average position days: {positionDays.Average():F}" });
            }
        }
    }
}
