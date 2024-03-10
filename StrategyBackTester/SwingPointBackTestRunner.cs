using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Stock.Data;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;
using Stock.Strategy;
using System.Diagnostics;
using IBApi;
using Stock.Strategies.EventArgs;

namespace StrategyBackTester
{
    internal class SwingPointBackTestRunner
    {
        private readonly IStrategy _strategy;
        private readonly List<AutomateOrder> _trackedOrder;

        public SwingPointBackTestRunner()
        {
            _strategy = new SwingPointsStrategy();
            _trackedOrder = new List<AutomateOrder>();
            _strategy.OrderCreated += Strategy_OrderCreated;
        }

        private void Strategy_OrderCreated(object sender, OrderEventArgs e)
        {
            var order = e.Order;

            if (order.Price.Date.Date == DateTime.Now.Date && !_trackedOrder.Contains(order))
            {
                Show(order);
            }

            _trackedOrder.Add(order);

            Log($"Order created: {order.Ticker} - {order.Time} - {order.Type} - {order.Price.Close} - {order.Quantity}");
        }

        private void Show(AutomateOrder order)
        {
            var toastContent = new ToastContentBuilder()
                .AddText($"New order created: {order.Ticker} - {order.Action} {order.Type}")
                .AddText($"Price: {order.Price.Close}")
                .AddText($"At EST: {order.Price.Date:f}");

            toastContent.Show();
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        public async Task Run()
        {
            try
            {
                var tickerBatch = new[] { TickersToTrade.POPULAR_TICKERS };

#if DEBUG
                var timeframes = new[] { Timeframe.Minute15 };
                var numberOfCandlesticksToLookBacks = new[] { 14 };
#else
                var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1, Timeframe.Daily };
                var numberOfCandlesticksToLookBacks = new[] {  15, 30  };
#endif

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
                                NumberOfSwingPointsToLookBack = 6,
                                NumberOfCandlesticksToLookBack = numberOfCandlestickToLookBack,
                                NumberOfCandlesticksToSkipAfterSwingPoint = 2
                            };

                            var strategyPath = Path.Combine(outputPath, $"lookback-{numberOfCandlestickToLookBack}");

                            await Parallel.ForEachAsync(tickers, parallelOptions, async (ticker, token) => {
                                decimal initialCap = 2000;
                                decimal cap = initialCap;

                                Log($"Running {nameof(SwingPointsStrategy)} for {ticker} at {timeframe} with {numberOfCandlestickToLookBack} lookback");

                                IList<AutomateOrder>? orders = null;
                                var repo = new StockDataRepository();
                                if (timeframe == Timeframe.Daily)
                                {
                                    var prices = await repo.GetStockData(ticker, timeframe, DateTime.Now.AddYears(-5), DateTime.Now);
                                    orders = _strategy.Run(ticker, prices.ToList(), swingPointStrategyParameter);
                                }
                                else
                                {
                                    var prices = await repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-3), DateTime.Now.AddDays(-7));
                                    orders = _strategy.Run(ticker, prices.ToList(), swingPointStrategyParameter);
                                }

                                if (orders == null || orders.Count < 2)
                                {
                                    return;
                                }

                                Log($"Finished running {nameof(SwingPointsStrategy)} for {ticker} at {timeframe} with {numberOfCandlestickToLookBack} lookback");

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

                                Log($"Writing to file {filePath}");

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

                                    if (previousOrder.Type == OrderPosition.Long)
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
                            $"Description: {_strategy.Description}",
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
            catch(Exception ex)
            {
                Log(ex.ToString());
            }
        }
    }
}
