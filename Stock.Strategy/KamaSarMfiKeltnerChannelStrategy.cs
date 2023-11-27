using Skender.Stock.Indicators;
using Stock.DataProvider;
using Stock.Shared.Helpers;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using System.Text;

namespace Stock.Strategy
{
    public class KamaSarMfiKeltnerChannelStrategy : IStrategy
    {
        public string Description => string.Empty;

        public IList<Order> Run(string ticker, IStrategyParameter strategyParameter, DateTime from, Timeframe timeframe = Timeframe.Daily)
        {
            KamaSarMfiKeltnerChannelParameter param = (KamaSarMfiKeltnerChannelParameter)strategyParameter;
            var dataProvider = new FmpStockDataProvider();
            var prices = dataProvider.CollectData(ticker, timeframe, from).Result;

            if (prices == null || prices.Count < 155)
            {
                return new List<Order>();
            }

            prices = prices.Reverse().ToList();

            var orders = new List<Order>();
            var kamaFast = prices.GetKama(param.Kama14Parameter.KamaPeriod, param.Kama14Parameter.KamaFastPeriod, param.Kama14Parameter.KamaSlowPeriod).ToArray();
            var kamaSlow = prices.GetKama(param.Kama75Parameter.KamaPeriod, param.Kama75Parameter.KamaFastPeriod, param.Kama75Parameter.KamaSlowPeriod).ToArray();
            var sar = prices.GetParabolicSar(param.SarParameter.SarAcceleration, param.SarParameter.SarMaxAcceleration).ToArray();
            var mfis = prices.GetMfi(param.MfiParameter.MfiPeriod).ToArray();
            var keltners = prices.GetKeltner(param.KeltnerParameter.KeltnerPeriod, param.KeltnerParameter.KeltnerMultiplier, param.KeltnerParameter.KeltnerAtrPeriod).ToArray();

            var lastOrderIndex = 0;
            for (var i = 250; i < prices.Count; i++) // start from 155 to make sure we have enough data
            {
                var price = prices[i];
                var date = price.Date;
                var close = price.Close;
                var kamaValue = kamaFast[i].Kama;
                var sarValue = sar[i].Sar;

                // kama check
                var lastNDayClose = prices.Skip(i - param.LastNDay1).Take(param.LastNDay1).Select(x => x.Close).ToList();
                var lastNDayKama = kamaFast.Skip(i - param.LastNDay1).Take(param.LastNDay1).Select(x => (decimal)x.Kama!).ToList();
                var priceCrossKama = CrossDirectionDetector.GetCrossDirection(lastNDayClose, lastNDayKama);
                var priceOpenAndCloseAboveKama = (double)price.Open >= kamaValue && (double)price.Close >= kamaValue;
                var priceOpenAndCloseUnderKama = (double)price.Open <= kamaValue && (double)price.Close <= kamaValue;
                var priceCompletelyAboveKamaSlow = (double)price.Open >= kamaSlow[i].Kama && (double)price.Close >= kamaSlow[i].Kama;
                var priceCompletelyUnderKamaSlow = (double)price.Open <= kamaSlow[i].Kama && (double)price.Close <= kamaSlow[i].Kama;

                // sar check
                var sarLowerThanPrice = (decimal)sarValue! < close;
                var sarGreaterThanPrice = (decimal)sarValue! > close;
                var sarReverseLastNDay = sar.Skip(i - param.LastNDay2).Take(param.LastNDay2).Any(x => x.IsReversal ?? false); // make sure sar is still fresh
                var isReverse = sar[i].IsReversal ?? false;

                // mfi check
                var bearishMfi = mfis[i].Mfi < param.MfiParameter.MiddleLimit && mfis[i].Mfi > param.MfiParameter.LowerLimit; // not too oversold
                var bullishMfi = mfis[i].Mfi > param.MfiParameter.MiddleLimit && mfis[i].Mfi < param.MfiParameter.UpperLimit; // not too overbought

                // kelner check
                // +1 so that we check for the current price
                var upperKeltnerInsideAnyPrice = prices.Skip(i - param.LastNDay2).Take(param.LastNDay2 + 1).Any(x => (decimal)keltners[i].UpperBand! > x.Low && (decimal)keltners[i].UpperBand! < x.High);
                var lowerKeltnerInsideAnyPrice = prices.Skip(i - param.LastNDay2).Take(param.LastNDay2 + 1).Any(x => (decimal)keltners[i].LowerBand! > x.Low && (decimal)keltners[i].LowerBand! < x.High);
                var anyPriceHigherThanUpperKeltner = prices.Skip(i - param.LastNDay2).Take(param.LastNDay2 + 1).Any(x => x.Low > (decimal)keltners[i].UpperBand!);
                var anyPriceLowerThanLowerKeltner = prices.Skip(i - param.LastNDay2).Take(param.LastNDay2 + 1).Any(x => x.High < (decimal)keltners[i].LowerBand!);
                var upperKeltnerInsidePrice = (decimal)keltners[i].UpperBand! > price.Low && (decimal)keltners[i].UpperBand! < price.High;
                var priceHigherThanUpperKeltner = price.Low > (decimal)keltners[i].UpperBand!;
                var lowerKeltnerInsidePrice = (decimal)keltners[i].LowerBand! > price.Low && (decimal)keltners[i].LowerBand! < price.High;
                var priceLowerThanLowerKeltner = price.High < (decimal)keltners[i].LowerBand!;
                var priceTouchOrCrossUpperKeltner = upperKeltnerInsidePrice || priceHigherThanUpperKeltner;
                var priceTouchOrCrossLowerKeltner = lowerKeltnerInsidePrice || priceLowerThanLowerKeltner;
                
                // price check
                var greenCandle = price.Open < price.Close;
                var redCandle = price.Open > price.Close;

                // exit signal
                var lastOrder = orders.LastOrDefault();
                var kamaExitSignal = false;
                var lowestPriceInLastNDays = prices.Skip(i - param.LastNDay1).Take(param.LastNDay1).Min(x => x.Low);
                var highestPriceInLastNDays = prices.Skip(i - param.LastNDay1).Take(param.LastNDay1).Min(x => x.High);
                var priceExitSignal = false;
                if (lastOrder != null && lastOrder.Action == EnterSignal.Open && lastOrder.Time.CompareTo(date) != 0)
                {
                    var lastOrderType = lastOrder.Type;
                    if (lastOrderType == OrderType.Long)
                    {
                        priceExitSignal = price.Close < lowestPriceInLastNDays;
                    }
                    else if (lastOrderType == OrderType.Short)
                    {
                        priceExitSignal = price.Close > highestPriceInLastNDays;
                    }
                }
                if (lastOrder != null && lastOrder.Action == EnterSignal.Open && lastOrder.Time.CompareTo(date) != 0)
                {
                    var lastOrderType = lastOrder.Type;
                    if (lastOrderType == OrderType.Long)
                    {
                        kamaExitSignal = price.Close < (decimal)kamaValue!;
                    }
                    else if (lastOrderType == OrderType.Short)
                    {
                        kamaExitSignal = price.Close > (decimal)kamaValue!;
                    }
                }

                var exitSignal = kamaExitSignal
                    || priceExitSignal; 

                // TODO: check for touching kama from below or above
                if (priceOpenAndCloseAboveKama 
                    && priceCrossKama == CrossDirection.CROSS_ABOVE
                    && priceCompletelyAboveKamaSlow
                    && sarReverseLastNDay 
                    && sarLowerThanPrice 
                    && bullishMfi
                    && !upperKeltnerInsideAnyPrice
                    && !anyPriceHigherThanUpperKeltner)
                {
                    if (i - lastOrderIndex <= 5)
                    {
                        continue;
                    }

                    lastOrderIndex = i;

                    var order = new Order
                    {
                        Ticker = ticker,
                        Time = date,
                        Type = OrderType.Long,
                        Price = price,
                        Quantity = 1,
                        Action = EnterSignal.Open,
                        Reason = $"Price cross above Kama"
                    };
                    AddIfLastOrderClose(orders, order);
                }
                else if (priceOpenAndCloseUnderKama 
                    && priceCrossKama == CrossDirection.CROSS_BELOW 
                    && priceCompletelyUnderKamaSlow
                    && sarReverseLastNDay 
                    && sarGreaterThanPrice 
                    && bearishMfi
                    && !lowerKeltnerInsideAnyPrice 
                    && !anyPriceLowerThanLowerKeltner)
                {
                    if (i - lastOrderIndex <= 5)
                    {
                        continue;
                    }

                    lastOrderIndex = i;

                    var order = new Order
                    {
                        Ticker = ticker,
                        Time = date,
                        Type = OrderType.Short,
                        Price = price,
                        Quantity = 1,
                        Action = EnterSignal.Open,
                        Reason = $"Price cross below Kama"
                    };
                    AddIfLastOrderClose(orders, order);
                }

                if (exitSignal)
                {
                    var reason = new StringBuilder();
                    if (priceTouchOrCrossLowerKeltner && greenCandle)
                    {
                        reason = reason.Append("Price touch or cross kelner lower and green candle");
                    }
                    if (priceTouchOrCrossUpperKeltner && redCandle)
                    {
                        if (reason.Length > 0)
                        {
                            reason = reason.Append('-');
                        }
                        reason = reason.Append("Price touch or cross kelner upper and red candle");
                    }
                    if (kamaExitSignal)
                    {
                        if (reason.Length > 0)
                        {
                            reason = reason.Append('-');
                        }
                        reason = reason.Append("Price cross kama");
                    }
                    if (priceExitSignal)
                    {
                        if (reason.Length > 0)
                        {
                            reason = reason.Append('-');
                        }
                        reason = reason.Append("Price close lower or higher previous days' price level");
                    }
                    CheckAndAddCloseOrder(orders, lastOrder, date, price, reason.ToString());
                }
            }

            return orders;
        }

        private static void CheckAndAddCloseOrder(List<Order> allOrderMade, Order? lastOrder, DateTime closeDate, IPrice closePrice, string reason)
        {
            if (lastOrder != null && lastOrder.Action == EnterSignal.Open && lastOrder.Time.CompareTo(closeDate) != 0)
            {
                var closeOrder = new Order
                {
                    Ticker = lastOrder.Ticker,
                    Time = closeDate,
                    Type = lastOrder.Type,
                    Price = closePrice,
                    Quantity = lastOrder.Quantity,
                    Action = EnterSignal.Close,
                    Reason = reason
                };

                allOrderMade.Add(closeOrder);
            }
        }

        /*
         * if last order is close, add new order
         * so that we don't have 2 open orders in a row
         * may reconsider this later as what if we want to add or average down/up
         */
        private static void AddIfLastOrderClose(List<Order> orders, Order order)
        {
            //var lastOrder = orders.LastOrDefault();
            //if (lastOrder == null)
            //{
            //    orders.Add(order);
            //}
            //else if(lastOrder.Action == EnterSignal.Close)
            //{
            //    orders.Add(order);
            //}
            orders.Add(order);
        }
    }
}