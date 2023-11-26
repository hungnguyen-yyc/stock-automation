using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;
using Stock.Strategies;
using Stock.Strategies.Parameters;
using Stock.Strategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrategyBackTester
{
    internal class BackTestRunner
    {
        public void Run()
        {
            var parameter = new KamaSarMfiKeltnerChannelParameter
            {
                Kama14Parameter = new KamaParameter
                {
                    KamaPeriod = 14,
                    KamaFastPeriod = 5,
                    KamaSlowPeriod = 30
                },
                Kama75Parameter = new KamaParameter
                {
                    KamaPeriod = 75,
                    KamaFastPeriod = 5,
                    KamaSlowPeriod = 30
                },
                SarParameter = new SarParameter
                {
                    SarMaxAcceleration = 0.2,
                    SarAcceleration = 0.1,
                    SarInitial = 0.1
                },
                MfiParameter = new MfiParameter
                {
                    MfiPeriod = 10,
                    UpperLimit = 80,
                    LowerLimit = 20,
                    MiddleLimit = 50
                },
                KeltnerParameter = new KeltnerParameter
                {
                    KeltnerPeriod = 20,
                    KeltnerMultiplier = 2,
                    KeltnerAtrPeriod = 10
                }
            };
            var tickers = new List<string> { "AMD", "MSFT", "RIVN", "AAPL", "GOOGL", "TSLA", "NVDA", "META", "AMZN", "COIN", "MARA", "RIOT", "RBLX", "SPY", "QQQ", "CAT", "DIS" };

            foreach (var ticker in tickers)
            {

                var orders = (new HmaBandSarStrategy()).Run(ticker, parameter, Timeframe.Daily, 5);

                if (orders == null || orders.Count < 2)
                {
                    continue;
                }

                var result = orders.Select(x => x.ToString());
                var fileName = $"{nameof(HmaBandSarStrategy)}-orders-{orders.FirstOrDefault()?.Ticker}-{DateTime.Now:yyyyMMdd-hhmmss}.txt";
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                File.AppendAllLines(fileName, result);

                var totalOfOrders = orders.Count;
                var wins = 0;
                var losses = 0;
                var positionDays = new List<int>();
                for (int i = 1; i < orders.Count; i += 2)
                {
                    var previousOrder = orders[i - 1];
                    var currentOrder = orders[i];
                    if (previousOrder.Type == OrderType.Long)
                    {
                        if (currentOrder.Price.Close > previousOrder.Price.Close)
                        {
                            wins++;
                        }
                        else
                        {
                            losses++;
                        }
                        positionDays.Add((currentOrder.Time - previousOrder.Time).Days);
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
                        }
                        positionDays.Add((currentOrder.Time - previousOrder.Time).Days);
                    }
                }

                File.AppendAllLines(fileName, new List<string> {
                    $"Total of orders: {totalOfOrders}", 
                    $"Total of wins: {wins}", 
                    $"Total of losses: {losses}", 
                    $"Average position days: {positionDays.Average():F}" });
            }
        }
    }
}
