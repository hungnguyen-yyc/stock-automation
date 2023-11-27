﻿using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;

namespace StrategyBackTester
{
    internal class SwingPointBackTestRunner : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
            // time to run 10 min before next candle hour, this is bc of FMP API
            // or we can set window task to run at 9:50 AM EST and set this back to 9:30 AM EST
            var marketOpen = new DateTime(now.Year, now.Month, now.Day, 9, 50, 0); 
            var marketClose = new DateTime(now.Year, now.Month, now.Day, 16, 0, 0);

            while (!stoppingToken.IsCancellationRequested && now < marketClose)
            {
                // run task every interval minutes from market open to market close
                if (now > marketOpen && now < marketClose)
                {
                    Console.WriteLine($"Running at {now}");
                    Run();
                    await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
                }
                now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
            }
            Environment.Exit(0);
        }

        public void Run()
        {
            var tickers = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "MARA", "RIOT", "RBLX", "SPY", "QQQ", "CAT", "DIS", "V", "AVGO", "SCHW" };
            var timeframes = new[] { Timeframe.Hour1, Timeframe.Hour4, Timeframe.Daily };
            var numberOfCandlesticksToLookBacks = new[] { 14, 21, 30 };
            var dateAtRun = DateTime.Now.ToString("yyyy-MM-dd");
            var timeAtRun = DateTime.Now.ToString("HH-mm");

            foreach (var timeframe in timeframes)
            {

#if DEBUG
                var outputPath = $"C:/Users/hnguyen/Documents/stock-back-test/debug/{nameof(SwingPointsStrategy)}/{dateAtRun}/{timeAtRun}/{timeframe}";
#else
                var outputPath = $"C:/Users/hnguyen/Documents/stock-back-test/{nameof(SwingPointsStrategy)}/{dateAtRun}/{timeAtRun}/{timeFrame}";
#endif

                foreach (var numberOfCandlestickToLookBack in numberOfCandlesticksToLookBacks)
                {
                    var swingPointStrategyParameter = new SwingPointStrategyParameter
                    {
                        NumberOfSwingPointsToLookBack = 4,
                        NumberOfCandlesticksToLookBack = numberOfCandlestickToLookBack
                    };

                    var strategyPath = Path.Combine(outputPath, $"lookback-{numberOfCandlestickToLookBack}");
                    foreach (var ticker in tickers)
                    {
                        var strategy = new SwingPointsStrategy();
                        var orders = strategy.Run(ticker, swingPointStrategyParameter, DateTime.Now.AddYears(-5), timeframe);

                        if (orders == null || orders.Count < 2)
                        {
                            continue;
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
                            var days = (currentOrder.Time - previousOrder.Time).Days;
                            File.AppendAllText(filePath, $"Change: {priceChange:C}({priceChangeInPercent:F}%) in {days} days.");

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
                            $"Timeframe: {timeframe}",
                            $"Ticker: {ticker}",
                            $"Description: {strategy.Description}",
                            $"Parameters: {JsonConvert.SerializeObject(swingPointStrategyParameter)}",
                            $"Total of orders: {totalOfOrders}",
                            $"Total of positions: {wins + losses}",
                            $"Total of wins: {wins}",
                            $"Total of losses: {losses}",
                            $"Win rate: {wins / (decimal)(wins + losses) * 100:F}%",
                            $"Average position days: {positionDays.Average():F}" });
                        }
                }
            }
        }
    }
}
