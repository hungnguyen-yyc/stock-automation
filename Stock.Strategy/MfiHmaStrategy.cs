using Skender.Stock.Indicators;
using Stock.DataProvider;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using Stock.Strategies.Trend;
using Stock.Strategy;

namespace Stock.Strategies
{
    /// <summary>
    /// This version of HMA Band strategy is based on the following rules:
    /// - TODO: update the rules
    /// </summary>
    public class MfiHmaStrategy : IStrategy
    {
        public IList<Order> Run(string ticker, IStrategyParameter strategyParameter, DateTime from, Timeframe timeframe = Timeframe.Daily, int lastNDay1 = 5, int lastNDay2 = 3)
        {
            var dataProvider = new FmpStockDataProvider();
            var trendIdentifier = new TrendIdentifier();
            var prices = dataProvider.CollectData(ticker, Timeframe.Daily, from).Result;
            var orders = new List<Order>();

            if (prices == null || prices.Count < 155)
            {
                return orders;
            }

            List<Price> orderedPrices = prices.Reverse().ToList();
            List<SlowHmaBand> slowHmaBands = new List<SlowHmaBand>();

            var mfi = orderedPrices.GetMfi(10).ToArray();
            var hma50 = orderedPrices.GetHma(50).ToArray();
            var hma55 = orderedPrices.GetHma(55).ToArray();
            var hma60 = orderedPrices.GetHma(60).ToArray();
            var hma65 = orderedPrices.GetHma(65).ToArray();
            var swingLows = trendIdentifier.FindSwingLows(orderedPrices, 7);
            var swingHighs = trendIdentifier.FindSwingHighs(orderedPrices, 7);

            for (var i = 0; i < orderedPrices.Count; i++)
            {
                var hma50Value = hma50[i].Hma ?? 0.0;
                var hma55Value = hma55[i].Hma ?? 0.0;
                var hma60Value = hma60[i].Hma ?? 0.0;
                var hma65Value = hma65[i].Hma ?? 0.0;
                slowHmaBands.Add(new SlowHmaBand(orderedPrices[i].Date, (decimal)hma50Value, (decimal)hma55Value, (decimal)hma60Value, (decimal)hma65Value));
            }

            for (var i = 175; i < orderedPrices.Count; i++)
            {
                var price = orderedPrices[i];
                var lastOrder = orders.LastOrDefault();

                // 2 swing highs before price date
                var swingHighsBeforePriceDate = swingHighs
                    .Where(x => x.Date < orderedPrices[i].Date)
                    .OrderByDescending(x => x.Date)
                    .Take(2)
                    .OrderBy(x => x.Date)
                    .ToList();

                // 2 swing lows before price date
                var swingLowsBeforePriceDate = swingLows
                    .Where(x => x.Date < orderedPrices[i].Date)
                    .OrderByDescending(x => x.Date)
                    .Take(2)
                    .OrderBy(x => x.Date)
                    .ToList();

                var latestSwingHighBeforePriceDate = swingHighsBeforePriceDate.Last();
                var latestSwingLowBeforePriceDate = swingLowsBeforePriceDate.Last();
                var latestSwingLowAfterLatestSwingHigh = latestSwingHighBeforePriceDate.Date < latestSwingLowBeforePriceDate.Date;
                var latestSwingHighAfterLatestSwingLow = latestSwingHighBeforePriceDate.Date > latestSwingLowBeforePriceDate.Date;

                var isLowerSwingHigh = swingHighsBeforePriceDate.Count == 2 && swingHighsBeforePriceDate[0].High > swingHighsBeforePriceDate[1].High;
                var isHigherSwingHigh = swingHighsBeforePriceDate.Count == 2 && swingHighsBeforePriceDate[0].High < swingHighsBeforePriceDate[1].High;
                var isLowerSwingLow = swingLowsBeforePriceDate.Count == 2 && swingLowsBeforePriceDate[0].Low > swingLowsBeforePriceDate[1].Low;
                var isHigherSwingLow = swingLowsBeforePriceDate.Count == 2 && swingLowsBeforePriceDate[0].Low < swingLowsBeforePriceDate[1].Low;

                // check if mfi cross above 60
                var mfiCrossAbove60 = mfi[i].Mfi > 60 && mfi[i - 1].Mfi < 60;
                var mfiCrossBelow40 = mfi[i].Mfi < 40 && mfi[i - 1].Mfi > 40;
                var mfiCrossAbove50 = mfi[i].Mfi > 50 && mfi[i - 1].Mfi < 50;
                var mfiCrossBelow50 = mfi[i].Mfi < 50 && mfi[i - 1].Mfi > 50;

                // check if hma band is bullish or bearish
                var slowHmaBand = slowHmaBands[i];
                var isSlowHmaBandBullish = slowHmaBand.IsBullish;
                var isSlowHmaBandBearish = slowHmaBand.IsBearish;
                var priceRange = new NumericRange(orderedPrices[i].Low, orderedPrices[i].High);
                var slowHmaBandRange = new NumericRange(slowHmaBand.LowestValue, slowHmaBand.HighestValue);
                var priceTouchSlowHmaBand = RangesIntersect(priceRange, slowHmaBandRange);

                if (lastOrder == null || lastOrder.Action == EnterSignal.Close)
                {
                    var isBullishBySwingPoints = (isHigherSwingHigh || isHigherSwingLow);
                    var isBearishBySwingPoints = (isLowerSwingHigh || isLowerSwingLow);
                    if (mfiCrossAbove60 && isSlowHmaBandBullish && isBullishBySwingPoints && latestSwingLowAfterLatestSwingHigh && !priceTouchSlowHmaBand && !slowHmaBand.IsInterwine)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Action = EnterSignal.Open,
                            Time = orderedPrices[i].Date,
                            Price = orderedPrices[i],
                            Type = OrderType.Long,
                        });
                    }
                    else if (mfiCrossBelow40 && isSlowHmaBandBearish && isBearishBySwingPoints && latestSwingHighAfterLatestSwingLow && !priceTouchSlowHmaBand && !slowHmaBand.IsInterwine)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Action = EnterSignal.Open,
                            Time = orderedPrices[i].Date,
                            Price = orderedPrices[i],
                            Type = OrderType.Short,
                        });
                    }
                }

                // close order
                if (lastOrder != null && lastOrder.Action == EnterSignal.Open)
                {
                    if ((mfiCrossBelow50 || priceRange.End < slowHmaBandRange.Start) && lastOrder.Type == OrderType.Long)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Action = EnterSignal.Close,
                            Time = orderedPrices[i].Date,
                            Price = orderedPrices[i],
                            Type = lastOrder.Type,
                            Reason = $"MFI cross below 50 on {orderedPrices[i].Date:yyyy-MM-dd}"
                        });
                    }
                    else if (priceTouchSlowHmaBand)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Action = EnterSignal.Close,
                            Time = orderedPrices[i].Date,
                            Price = orderedPrices[i],
                            Type = lastOrder.Type,
                            Reason = $"Price touch slow HMA band on {orderedPrices[i].Date:yyyy-MM-dd}"
                        });
                    }
                    else if ((mfiCrossAbove50 || priceRange.Start < slowHmaBandRange.End)&& lastOrder.Type == OrderType.Short)
                    {
                        orders.Add(new Order
                        {
                            Ticker = ticker,
                            Action = EnterSignal.Close,
                            Time = orderedPrices[i].Date,
                            Price = orderedPrices[i],
                            Type = lastOrder.Type,
                            Reason = $"MFI cross above 50 on {orderedPrices[i].Date:yyyy-MM-dd}"
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
