﻿using Stock.Data;
using Stock.DataProvider;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using Stock.Strategies.Trend;
using Stock.Strategy;

namespace Stock.Strategies
{
    public class SwingPointsStrategy : IStrategy
    {
        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public async Task<IList<Order>> Run(string ticker, IStrategyParameter strategyParameter, DateTime from, DateTime to, Timeframe timeframe = Timeframe.Daily)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfSwingPointsToLookBack = parameter.NumberOfSwingPointsToLookBack;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var repo = new StockDataRepository();
            var trendIdentifier = new TrendIdentifier();
            var prices = await repo.GetStockData(ticker, timeframe, from);
            var orders = new List<Order>();

            if (prices == null || prices.Count < 155)
            {
                return orders;
            }

            List<Price> sortedPrices = prices.ToList();
            var swingLows = trendIdentifier.FindSwingLows(sortedPrices, numberOfCandlesticksToLookBack);
            var swingHighs = trendIdentifier.FindSwingHighs(sortedPrices, numberOfCandlesticksToLookBack);
            var minCount = Math.Min(swingLows.Count, swingHighs.Count);

            if (minCount <= numberOfSwingPointsToLookBack)
            {
                return orders;
            }

            swingLows = swingLows.Skip(swingLows.Count - minCount - numberOfSwingPointsToLookBack).ToList();
            swingHighs = swingHighs.Skip(swingHighs.Count - minCount - numberOfSwingPointsToLookBack).ToList();
            
            var firstSwingLow = swingLows[0];
            var firstSwingHigh = swingHighs[0];

            if (firstSwingLow.Date > firstSwingHigh.Date)
            {
                sortedPrices = sortedPrices.Where(x => x.Date >= firstSwingHigh.Date).ToList();
            }
            else
            {
                sortedPrices = sortedPrices.Where(x => x.Date >= firstSwingLow.Date).ToList();
            }

            var previousSwingLow = swingLows[0];
            var previousSwingHigh = swingHighs[0];
            for (int i = 0; i < sortedPrices.Count; i++)
            {
                var price = sortedPrices[i];
                var immediateSwingLowBeforePrice = swingLows.Where(x => x.Date < price.Date).OrderByDescending(x => x.Date).FirstOrDefault();
                var immediateSwingHighBeforePrice = swingHighs.Where(x => x.Date < price.Date).OrderByDescending(x => x.Date).FirstOrDefault();

                if (immediateSwingLowBeforePrice == null || immediateSwingHighBeforePrice == null)
                {
                    continue;
                }

                var indexOfImmediateSwingLowBeforePrice = sortedPrices.IndexOf(immediateSwingLowBeforePrice);
                var indexOfImmediateSwingHighBeforePrice = sortedPrices.IndexOf(immediateSwingHighBeforePrice);

                var lastorder = orders.LastOrDefault();
                //var daysAfterSwingLow = CalculateBusinessDays(immediateSwingLowBeforePrice.Date, price.Date);
                //var daysAfterSwingHigh = CalculateBusinessDays(immediateSwingHighBeforePrice.Date, price.Date);
                var daysAfterSwingLow = i - indexOfImmediateSwingLowBeforePrice;
                var daysAfterSwingHigh = i - indexOfImmediateSwingHighBeforePrice;

                if (lastorder == null || lastorder.Action == EnterSignal.Close)
                {
                    var previousPrice = sortedPrices[i - 1];
                    var orderSize = 100;
                    /**
                     * if price is closer to swing low we start checking for buy signal
                     * and vice versa.
                     **/
                    if (daysAfterSwingLow < daysAfterSwingHigh)
                    {
                        // sometimes data comes from FMP is not correct
                        // strange thing is that when we use trend identifier, we have less trades and less profits
                        //var swingPointsBeforePrice = swingLows.Where(x => x.Date < price.Date).OrderByDescending(x => x.Date).Take(numberOfSwingPointsToLookBack).ToList();
                        //var swingPointsTrend = trendIdentifier.DetermineSwingTrend(swingPointsBeforePrice.Select(p => p.Low).ToList(), numberOfSwingPointsToLookBack);
                        //var isValidTrend = swingPointsTrend == TrendDirection.Uptrend || swingPointsTrend == TrendDirection.ReversalToUptrend;
                        var isValidTrend = true;

                        // make sure that price is higher than previous swing low to confirm the reversal
                        var priceBetweenSwingLowAndCurrentPrice = sortedPrices.Where(x => x.Date > immediateSwingLowBeforePrice.Date && x.Date < price.Date).ToList();
                        var confirmedSwingLowByPreviousPrice = priceBetweenSwingLowAndCurrentPrice.All(x => x.Low > immediateSwingLowBeforePrice.Low);

                        if (daysAfterSwingLow == parameter.NumberOfCandlesticksToSkipAfterSwingPoint && confirmedSwingLowByPreviousPrice && isValidTrend)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Long,
                                Price = price,
                                Quantity = orderSize,
                                Action = EnterSignal.Open,
                                Time = price.Date
                            });
                            previousSwingLow = immediateSwingLowBeforePrice;
                            previousSwingHigh = immediateSwingHighBeforePrice;
                        }
                    }
                    else if (daysAfterSwingHigh < daysAfterSwingLow)
                    {
                        //var swingPointsBeforePrice = swingHighs.Where(x => x.Date < price.Date).OrderByDescending(x => x.Date).Take(numberOfSwingPointsToLookBack).ToList();
                        //var swingPointsTrend = trendIdentifier.DetermineSwingTrend(swingPointsBeforePrice.Select(p => p.High).ToList(), numberOfSwingPointsToLookBack);
                        //var isValidTrend = swingPointsTrend == TrendDirection.Downtrend || swingPointsTrend == TrendDirection.ReversalToDowntrend;
                        var isValidTrend = true;

                        var priceBetweenSwingHighAndCurrentPrice = sortedPrices.Where(x => x.Date > immediateSwingHighBeforePrice.Date && x.Date < price.Date).ToList();
                        var confirmedSwingHighByPreviousPrice = priceBetweenSwingHighAndCurrentPrice.All(x => x.High < immediateSwingHighBeforePrice.High);

                        if (daysAfterSwingHigh == parameter.NumberOfCandlesticksToSkipAfterSwingPoint && confirmedSwingHighByPreviousPrice && isValidTrend)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Short,
                                Price = price,
                                Quantity = orderSize,
                                Action = EnterSignal.Open,
                                Time = price.Date
                            });
                            previousSwingLow = immediateSwingLowBeforePrice;
                            previousSwingHigh = immediateSwingHighBeforePrice;
                        }
                    }
                }

                // close order
                if (lastorder != null && lastorder.Action == EnterSignal.Open)
                {
                    if (lastorder.Type == OrderType.Long)
                    {
                        var newSwingHigh = previousSwingHigh.Date != immediateSwingHighBeforePrice.Date;

                        // close when current price is lower than immediate swing low before price
                        if (price.Low < immediateSwingLowBeforePrice.Low)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Long,
                                Price = price,
                                Quantity = lastorder.Quantity,
                                Action = EnterSignal.Close,
                                Time = price.Date
                            });
                        }
                        else if (newSwingHigh)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Long,
                                Price = price,
                                Quantity = lastorder.Quantity,
                                Action = EnterSignal.Close,
                                Time = price.Date
                            });
                        }
                    }
                    else
                    {
                        var newSwingLow = previousSwingLow.Date != immediateSwingLowBeforePrice.Date;

                        // close when current price is higher than immediate swing high before price
                        if (price.High > immediateSwingHighBeforePrice.High)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Short,
                                Price = price,
                                Quantity = 1,
                                Action = EnterSignal.Close,
                                Time = price.Date
                            });
                        }
                        else if (newSwingLow)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Short,
                                Price = price,
                                Quantity = 1,
                                Action = EnterSignal.Close,
                                Time = price.Date
                            });
                        }
                    }
                }
            }

            return orders;
        }

        public int CalculateBusinessDays(DateTime startDate, DateTime endDate)
        {
            int businessDays = 0;

            while (startDate < endDate)
            {
                if (startDate.DayOfWeek != DayOfWeek.Saturday && startDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    businessDays++;
                }

                startDate = startDate.AddDays(1);
            }

            return businessDays;
        }
    }
}
