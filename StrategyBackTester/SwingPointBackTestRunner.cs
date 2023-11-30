using Newtonsoft.Json;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;
using System.Diagnostics;

namespace StrategyBackTester
{
    internal class SwingPointBackTestRunner
    {
        public async Task Run()
        {
            var tickerBatch = new[] { TickersToTrade.CHEAP_TICKERS, TickersToTrade.POPULAR_TICKERS };
            var timeframes = new[] { Timeframe.Hour1, Timeframe.Daily, Timeframe.Minute15 };
            var numberOfCandlesticksToLookBacks = new[] {  30, 15  };
            var dateAtRun = DateTime.Now.ToString("yyyy-MM-dd");
            var timeAtRun = DateTime.Now.ToString("HH-mm");
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(tickerBatch, parallelOptions, async (tickers, token) =>
            {
                await Parallel.ForEachAsync(timeframes, parallelOptions, async (timeframe, token) =>
                {
#if DEBUG
                    var outputPath = $"C:/Users/hnguyen/Documents/stock-back-test/debug/{nameof(SwingPointsStrategy)}/{dateAtRun}/{timeAtRun}/{timeframe}";
#else
                    var outputPath = $"C:/Users/hnguyen/Documents/stock-back-test/release/{nameof(SwingPointsStrategy)}/{dateAtRun}/{timeAtRun}/{timeframe}";
#endif

                    await Parallel.ForEachAsync(numberOfCandlesticksToLookBacks, parallelOptions, async (numberOfCandlestickToLookBack, token) =>
                    {
                        var swingPointStrategyParameter = new SwingPointStrategyParameter
                        {
                            NumberOfSwingPointsToLookBack = 3,
                            NumberOfCandlesticksToLookBack = numberOfCandlestickToLookBack,
                            NumberOfCandlesticksToSkipAfterSwingPoint = 2
                        };

                        var strategyPath = Path.Combine(outputPath, $"lookback-{numberOfCandlestickToLookBack}");

                        await Parallel.ForEachAsync(tickers, parallelOptions, async (ticker, token) => {
                            decimal initialCap = 2000;
                            decimal cap = initialCap;
                            Debug.WriteLine($"Running {nameof(SwingPointsStrategy)} for {ticker} at {timeframe} with {numberOfCandlestickToLookBack} lookback");
                            var strategy = new SwingPointsStrategy();

                            IList<Order>? orders = null;
                            if (timeframe == Timeframe.Daily)
                            {
                                orders = await strategy.Run(ticker, swingPointStrategyParameter, DateTime.Now.AddYears(-10), DateTime.Now, timeframe);
                            }
                            else
                            {
                                orders = await strategy.Run(ticker, swingPointStrategyParameter, DateTime.Now.AddYears(-10), DateTime.Now, timeframe);
                            }

                            if (orders == null || orders.Count < 2)
                            {
                                return;
                            }

                            var fileNameWithoutExtension = $"{orders.FirstOrDefault()?.Ticker}-{DateTime.Now:yyyyMMdd-hhmmss}";
                            var fileName = $"{fileNameWithoutExtension}.txt";

                            if (!Directory.Exists(strategyPath))
                            {
                                Directory.CreateDirectory(strategyPath);
                            }

                            var filePath = Path.Combine(strategyPath, fileName);
                            var filePathResult = Path.Combine(strategyPath, $"{fileNameWithoutExtension}-result.txt");
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }

                            var totalOfOrders = orders.Count;
                            var wins = 0;
                            var losses = 0;
                            var positionDays = new List<int>();
                            var percentageChange = new List<decimal>();
                            for (int i = 1; i < orders.Count; i++)
                            {
                                var previousOrder = orders[i - 1];
                                var currentOrder = orders[i];

                                if (i % 2 == 0 && i == orders.Count - 1)
                                {
                                    File.AppendAllText(filePath, currentOrder.ToString());
                                    continue;
                                }

                                if (i % 2 == 0)
                                {
                                    continue;
                                }

                                File.AppendAllText(filePath, previousOrder.ToString());
                                File.AppendAllText(filePath, "\n");
                                File.AppendAllText(filePath, currentOrder.ToString());

                                var priceChange = currentOrder.Price.Close - previousOrder.Price.Close;
                                var priceChangeInPercent = (currentOrder.Price.Close - previousOrder.Price.Close) / previousOrder.Price.Close * 100;
                                percentageChange.Add(priceChangeInPercent);

                                var days = (currentOrder.Time - previousOrder.Time).Days;

                                File.AppendAllText(filePath, $";{priceChange:C};{priceChangeInPercent:F}%; {days} days");

                                if (previousOrder.Type == OrderType.Long)
                                {
                                    if (currentOrder.Price.Close > previousOrder.Price.Close)
                                    {
                                        wins++;
                                        File.AppendAllText(filePath, ";W");
                                    }
                                    else
                                    {
                                        losses++;
                                        File.AppendAllText(filePath, ";L");
                                    }
                                    cap = cap + priceChange * currentOrder.Quantity;
                                }
                                else
                                {
                                    if (currentOrder.Price.Close < previousOrder.Price.Close)
                                    {
                                        wins++;
                                        File.AppendAllText(filePath, ";W");
                                    }
                                    else
                                    {
                                        losses++;
                                        File.AppendAllText(filePath, ";L");
                                    }
                                    cap = cap - priceChange * currentOrder.Quantity;
                                }

                                File.AppendAllText(filePath, "\n");
                                positionDays.Add((currentOrder.Time - previousOrder.Time).Days);
                            }

                            File.AppendAllLines(filePathResult, new List<string> {
                            $"Strategy: {nameof(SwingPointsStrategy)}",
                            $"Timeframe: {timeframe}",
                            $"Ticker: {ticker}",
                            $"Description: {strategy.Description}",
                            $"Parameters: {JsonConvert.SerializeObject(swingPointStrategyParameter)}",
                            $"Total of orders: {totalOfOrders}",
                            $"Total of positions: {wins + losses}",
                            $"Total of wins: {wins}",
                            $"Total of losses: {losses}",
                            $"Win rate: {wins / (decimal)(wins + losses) * 100:F}%",
                            $"Capital ({initialCap:C}): {cap:C}",
                            $"Max percentage change: {percentageChange.Max():F}%",
                            $"Min percentage change: {percentageChange.Min():F}%",
                            $"Percentage change: {percentageChange.Average():F}%",
                            $"Max position days: {positionDays.Max()}",
                            $"Min position days: {positionDays.Min()}",
                            $"Average position days: {positionDays.Average():F}" });
                        });
                    });
                });
            });
        }
    }
}
