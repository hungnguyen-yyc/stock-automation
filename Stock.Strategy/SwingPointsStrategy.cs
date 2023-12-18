using Stock.Shared.Models;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using Stock.Strategies.Trend;
using Stock.Strategy;

namespace Stock.Strategies
{
    public class SwingPointsStrategy : IStrategy
    {
        public event OrderEventHandler OrderCreated;
        public event AlertEventHandler AlertCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public IList<Order> Run(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfSwingPointsToLookBack = parameter.NumberOfSwingPointsToLookBack;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var orders = new List<Order>();

            if (ascSortedByDatePrice == null || ascSortedByDatePrice.Count < 155)
            {
                return orders;
            }

            List<Price> sortedPrices = ascSortedByDatePrice.ToList();
            var swingLows = SwingPointAnalyzer.FindSwingLows(sortedPrices, numberOfCandlesticksToLookBack);
            var swingHighs = SwingPointAnalyzer.FindSwingHighs(sortedPrices, numberOfCandlesticksToLookBack);
            var minCount = Math.Min(swingLows.Count, swingHighs.Count);

            if (minCount <= numberOfSwingPointsToLookBack)
            {
                return orders;
            }

            for (int i = 155; i < ascSortedByDatePrice.Count; i++)
            {
                var rangeToCheck = ascSortedByDatePrice.Skip(i).Take(14).ToList();
                var channel = SwingPointAnalyzer.CheckRunningCandlesFormingChannel(rangeToCheck, 14, 3);
                if (channel != null)
                {
                    var order = new Order
                    {
                        Ticker = ticker,
                        Type = OrderType.Long,
                        Price = rangeToCheck[0],
                        Quantity = 100,
                        Action = OrderAction.Open,
                        Time = rangeToCheck[0].Date
                    };
                    orders.Add(order);
                    OnOrderCreated(new OrderEventArgs(order));
                }
            }

            var highLines = SwingPointAnalyzer.GetTrendlines(sortedPrices, numberOfCandlesticksToLookBack, 3, true);
            var lowLines = SwingPointAnalyzer.GetTrendlines(sortedPrices, numberOfCandlesticksToLookBack, 3, false);
            var bottoms = SwingPointAnalyzer.GetNBottoms(sortedPrices, numberOfCandlesticksToLookBack);
            var tops = SwingPointAnalyzer.GetNTops(sortedPrices, numberOfCandlesticksToLookBack);
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

                if (lastorder == null || lastorder.Action == OrderAction.Close)
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
                        var priceBetweenSwingLowAndCurrentPrice = sortedPrices.Where(x => x.Date > immediateSwingLowBeforePrice.Date && x.Date <= price.Date).ToList();
                        var confirmedSwingLowByPreviousPrice = priceBetweenSwingLowAndCurrentPrice.All(x => x.Low > immediateSwingLowBeforePrice.Low);

                        if (daysAfterSwingLow == parameter.NumberOfCandlesticksToSkipAfterSwingPoint && confirmedSwingLowByPreviousPrice && isValidTrend)
                        {
                            var order = new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Long,
                                Price = price,
                                Quantity = orderSize,
                                Action = OrderAction.Open,
                                Time = price.Date
                            };
                            orders.Add(order);
                            previousSwingLow = immediateSwingLowBeforePrice;
                            previousSwingHigh = immediateSwingHighBeforePrice;

                            OnOrderCreated(new OrderEventArgs(order));
                        }
                    }
                    else if (daysAfterSwingHigh < daysAfterSwingLow)
                    {
                        //var swingPointsBeforePrice = swingHighs.Where(x => x.Date < price.Date).OrderByDescending(x => x.Date).Take(numberOfSwingPointsToLookBack).ToList();
                        //var swingPointsTrend = trendIdentifier.DetermineSwingTrend(swingPointsBeforePrice.Select(p => p.High).ToList(), numberOfSwingPointsToLookBack);
                        //var isValidTrend = swingPointsTrend == TrendDirection.Downtrend || swingPointsTrend == TrendDirection.ReversalToDowntrend;
                        var isValidTrend = true;

                        var priceBetweenSwingHighAndCurrentPrice = sortedPrices.Where(x => x.Date > immediateSwingHighBeforePrice.Date && x.Date <= price.Date).ToList();
                        var confirmedSwingHighByPreviousPrice = priceBetweenSwingHighAndCurrentPrice.All(x => x.High < immediateSwingHighBeforePrice.High);

                        if (daysAfterSwingHigh == parameter.NumberOfCandlesticksToSkipAfterSwingPoint && confirmedSwingHighByPreviousPrice && isValidTrend)
                        {
                            var order = new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Short,
                                Price = price,
                                Quantity = orderSize,
                                Action = OrderAction.Open,
                                Time = price.Date
                            };
                            orders.Add(order);
                            previousSwingLow = immediateSwingLowBeforePrice;
                            previousSwingHigh = immediateSwingHighBeforePrice;

                            OnOrderCreated(new OrderEventArgs(order));
                        }
                    }
                }

                // close order
                if (lastorder != null && lastorder.Action == OrderAction.Open)
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
                                Action = OrderAction.Close,
                                Time = price.Date
                            });

                            OnOrderCreated(new OrderEventArgs(lastorder));
                        }
                        else if (newSwingHigh)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Long,
                                Price = price,
                                Quantity = lastorder.Quantity,
                                Action = OrderAction.Close,
                                Time = price.Date
                            });

                            OnOrderCreated(new OrderEventArgs(lastorder));
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
                                Quantity = lastorder.Quantity,
                                Action = OrderAction.Close,
                                Time = price.Date
                            });

                            OnOrderCreated(new OrderEventArgs(lastorder));
                        }
                        else if (newSwingLow)
                        {
                            orders.Add(new Order
                            {
                                Ticker = ticker,
                                Type = OrderType.Short,
                                Price = price,
                                Quantity = lastorder.Quantity,
                                Action = OrderAction.Close,
                                Time = price.Date
                            });

                            OnOrderCreated(new OrderEventArgs(lastorder));
                        }
                    }
                }
            }

            return orders;
        }

        protected virtual void OnOrderCreated(OrderEventArgs e)
        {
            OrderCreated?.Invoke(this, e);
        }

        protected virtual void OnAlertCreated(AlertEventArgs e)
        {
            AlertCreated?.Invoke(this, e);
        }
    }
}
