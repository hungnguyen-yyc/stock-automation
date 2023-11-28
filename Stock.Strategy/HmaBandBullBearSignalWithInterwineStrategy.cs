using Skender.Stock.Indicators;
using Stock.DataProvider;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using Stock.Strategies.Trend;
using Stock.Strategy;

namespace Stock.Strategies
{
    public class HmaBandBullBearSignalWithInterwineStrategy : IStrategy
    {
        public string Description => "This version of HMA Band strategy is based on the following rules:\r\n" +
            "- Slow HMA band is switch to bullist or bearish\r\n" +
            "- Slow HMA band is interwine or not\r\n" +
            "- Work quite well with AMD but failed with the rest of the tickers";

        public async Task<IList<Order>> Run(string ticker, IStrategyParameter strategyParameter, DateTime from, DateTime to, Timeframe timeframe = Timeframe.Daily)
        {
            var dataProvider = new FmpStockDataProvider();
            var trendIdentifier = new TrendIdentifier();
            var prices = await dataProvider.CollectData(ticker, Timeframe.Daily, from);
            var orders = new List<Order>();

            if (prices == null || prices.Count < 155)
            {
                return orders;
            }

            List<Price> orderedPrices = prices.Reverse().ToList();
            List<SlowHmaBand> slowHmaBands = new List<SlowHmaBand>();
            List<FastHmaBand> fastHmaBands = new List<FastHmaBand>();

            var hma10 = orderedPrices.GetHma(10).ToArray();
            var hma15 = orderedPrices.GetHma(15).ToArray();
            var hma20 = orderedPrices.GetHma(20).ToArray();
            var hma25 = orderedPrices.GetHma(25).ToArray();

            for (var i = 0; i < orderedPrices.Count; i++)
            {
                var hma10Value = hma10[i].Hma ?? 0.0;
                var hma15Value = hma15[i].Hma ?? 0.0;
                var hma20Value = hma20[i].Hma ?? 0.0;
                var hma25Value = hma25[i].Hma ?? 0.0;
                fastHmaBands.Add(new FastHmaBand(orderedPrices[i].Date, (decimal)hma10Value, (decimal)hma15Value, (decimal)hma20Value, (decimal)hma25Value));
            }

            var hma50 = orderedPrices.GetHma(50).ToArray();
            var hma55 = orderedPrices.GetHma(55).ToArray();
            var hma60 = orderedPrices.GetHma(60).ToArray();
            var hma65 = orderedPrices.GetHma(65).ToArray();

            for (var i = 0; i < orderedPrices.Count; i++)
            {
                var hma50Value = hma50[i].Hma ?? 0.0;
                var hma55Value = hma55[i].Hma ?? 0.0;
                var hma60Value = hma60[i].Hma ?? 0.0;
                var hma65Value = hma65[i].Hma ?? 0.0;
                slowHmaBands.Add(new SlowHmaBand(orderedPrices[i].Date, (decimal)hma50Value, (decimal)hma55Value, (decimal)hma60Value, (decimal)hma65Value));
            }

            for (var i = 155; i < orderedPrices.Count; i++)
            {
                var slowHmaBand = slowHmaBands[i];
                var previousSlowHmaBand = slowHmaBands[i - 1];
                var fastHmaBand = fastHmaBands[i];
                var price = orderedPrices[i];
                var priceTouchingSlowHmaBand = RangesIntersect(new NumericRange(price.Low, price.High), new NumericRange(slowHmaBand.LowestValue, slowHmaBand.HighestValue));

                if (priceTouchingSlowHmaBand)
                {
                    var lastOrder = orders.LastOrDefault();
                    if (lastOrder != null && lastOrder.Action == EnterSignal.Open)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Type = lastOrder.Type,
                            Price = price,
                            Quantity = 1,
                            Action = EnterSignal.Close,
                            Time = price.Date,
                            Reason = $"Price touching the slow HMA band on {price.Date:yyyy-MM-dd}"
                        });
                    }
                }
                else
                {
                    // check if Price crosses above the slow HMA band
                    if (slowHmaBand.IsBullish && (!slowHmaBand.IsInterwine && previousSlowHmaBand.IsInterwine) && price.Close > slowHmaBand.HighestValue)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Type = OrderType.Long,
                            Price = price,
                            Quantity = 1,
                            Action = EnterSignal.Open,
                            Time = price.Date,
                            Reason = $"Price crosses above the slow HMA band on {price.Date:yyyy-MM-dd}"
                        });
                    }

                    // check if Price crosses below the slow HMA band
                    if (slowHmaBand.IsBearish && (!slowHmaBand.IsInterwine && previousSlowHmaBand.IsInterwine) && price.Close < slowHmaBand.LowestValue)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Type = OrderType.Short,
                            Price = price,
                            Quantity = 1,
                            Action = EnterSignal.Open,
                            Time = price.Date,
                            Reason = $"Price crosses below the slow HMA band on {price.Date:yyyy-MM-dd}"
                        });
                    }
                }
            }

            return orders;
        }
        static bool RangesIntersect(NumericRange range1, NumericRange range2)
        {
            var intersect = range1.Start <= range2.Start && range1.End >= range2.End
                || range1.Start >= range2.Start && range1.End <= range2.End
                || range1.Start <= range2.Start && range1.End >= range2.Start
                || range1.Start <= range2.End && range1.End >= range2.End;
            return intersect;
        }

        private class NumericRange
        {
            public NumericRange(decimal start, decimal end)
            {
                Start = start;
                End = end;
            }

            public decimal Start { get; set; }
            public decimal End { get; set; }
        }

        private interface IHmaBand
        {
            public decimal HighestValue { get; }
            public decimal LowestValue { get; }
            public bool IsBullish { get; }
            public bool IsBearish { get; }
            public bool IsInterwine { get; }
        }

        private class FastHmaBand : IHmaBand
        {
            public FastHmaBand(DateTime date, decimal hma10, decimal hma15, decimal hma20, decimal hma25)
            {
                Date = date;
                Hma10 = hma10;
                Hma15 = hma15;
                Hma20 = hma20;
                Hma25 = hma25;
            }

            public DateTime Date { get; }
            public decimal Hma10 { get; }
            public decimal Hma15 { get; }
            public decimal Hma20 { get; }
            public decimal Hma25 { get; }

            public decimal HighestValue => Math.Max(Math.Max(Math.Max(Hma10, Hma15), Hma20), Hma25);

            public decimal LowestValue => Math.Min(Math.Min(Math.Min(Hma10, Hma15), Hma20), Hma25);

            public bool IsBullish => Hma10 > Hma15 && Hma15 > Hma20 && Hma20 > Hma25;

            public bool IsBearish => Hma10 < Hma15 && Hma15 < Hma20 && Hma20 < Hma25;

            public bool IsInterwine => (HighestValue - LowestValue) / LowestValue < 0.01m;
        }

        private class SlowHmaBand : IHmaBand
        {
            public SlowHmaBand(DateTime date, decimal hma50, decimal hma55, decimal hma60, decimal hma65)
            {
                Date = date;
                Hma50 = hma50;
                Hma55 = hma55;
                Hma60 = hma60;
                Hma65 = hma65;
            }

            public DateTime Date { get; }
            public decimal Hma50 { get; }
            public decimal Hma55 { get; }
            public decimal Hma60 { get; }
            public decimal Hma65 { get; }

            public decimal HighestValue => Math.Max(Math.Max(Math.Max(Hma50, Hma55), Hma60), Hma65);

            public decimal LowestValue => Math.Min(Math.Min(Math.Min(Hma50, Hma55), Hma60), Hma65);

            public bool IsBullish => Hma50 > Hma55 && Hma55 > Hma60 && Hma60 > Hma65;

            public bool IsBearish => Hma50 < Hma55 && Hma55 < Hma60 && Hma60 < Hma65;

            public bool IsInterwine => (HighestValue - LowestValue) / LowestValue < 0.01m;
        }
    }
}
