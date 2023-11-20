﻿using Skender.Stock.Indicators;
using Stock.Shared.Helpers;
using Stock.Shared.Models;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Stock.Strategy
{
    public class KamaSarMfiKeltnerChannelStrategy
    {
        public static IList<Order> Run(string ticker, KamaSarMfiKeltnerChannelParameter param, double gap = 2.5, int lastNDay = 5, Timeframe timeframe = Timeframe.Daily)
        {
            var stockData = new StockDataCollector();
            var prices = stockData.CollectData(ticker, timeframe, DateTime.Now.AddYears(-4)).Result;

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
                var lastNDayClose = prices.Skip(i - lastNDay).Take(lastNDay).Select(x => x.Close).ToList();
                var lastNDayKama = kamaFast.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)x.Kama!).ToList();
                var upperRange80 = prices.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)80).ToList();
                var lowerRange20 = prices.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)20).ToList();
                var priceCrossKama = CrossDirectionDetector.GetCrossDirection(lastNDayClose, lastNDayKama);
                var priceOpenAndCloseAboveKama = (double)price.Open >= kamaValue && (double)price.Close >= kamaValue;
                var priceOpenAndCloseUnderKama = (double)price.Open <= kamaValue && (double)price.Close <= kamaValue;
                var priceCompletelyAboveKamaSlow = (double)price.Open >= kamaSlow[i].Kama && (double)price.Close >= kamaSlow[i].Kama;
                var priceCompletelyUnderKamaSlow = (double)price.Open <= kamaSlow[i].Kama && (double)price.Close <= kamaSlow[i].Kama;

                // sar check
                var sarLowerThanPrice = (decimal)sarValue! < close;
                var sarGreaterThanPrice = (decimal)sarValue! > close;
                var sarReverseLastNDay = sar.Skip(i - 3).Take(3).Any(x => x.IsReversal ?? false); // make sure sar is still fresh
                var isReverse = sar[i].IsReversal ?? false;

                // mfi check
                var bearishMfi = mfis[i].Mfi < param.MfiParameter.MiddleLimit && mfis[i].Mfi > param.MfiParameter.LowerLimit; // not too oversold
                var bullishMfi = mfis[i].Mfi > param.MfiParameter.MiddleLimit && mfis[i].Mfi < param.MfiParameter.UpperLimit; // not too overbought

                // kelner check
                var keltnerUpperInsidePrice = (decimal)keltners[i].UpperBand! > price.Low && (decimal)keltners[i].UpperBand! < price.High;
                var priceHigherThanKelnerUpper = price.Low > (decimal)keltners[i].UpperBand!;
                var keltnerLowerInsidePrice = (decimal)keltners[i].LowerBand! > price.Low && (decimal)keltners[i].LowerBand! < price.High;
                var priceLowerThanKelnerLower = price.High < (decimal)keltners[i].LowerBand!;
                var priceTouchOrCrossKelnerUpper = keltnerUpperInsidePrice || priceHigherThanKelnerUpper;
                var priceTouchOrCrossKelnerLower = keltnerLowerInsidePrice || priceLowerThanKelnerLower;
                
                // price check
                var greenCandle = price.Open < price.Close;
                var redCandle = price.Open > price.Close;

                // TODO: should check for touching kama from below or above, or low of previous candle
                var exitSignal = (priceTouchOrCrossKelnerLower && greenCandle) // exit for short position when price touch or cross kelner lower and green candle
                    || (priceTouchOrCrossKelnerUpper && redCandle); // exit for long position when price touch or cross kelner upper and red candle

                // TODO: check for touching kama from below or above
                if (priceOpenAndCloseAboveKama 
                    && priceCrossKama == CrossDirection.CROSS_ABOVE
                    && priceCompletelyAboveKamaSlow
                    && sarReverseLastNDay 
                    && sarLowerThanPrice 
                    && bullishMfi)
                {
                    // TODO: so that we don't have too many orders, we check if the last order is closed
                    if (i - lastOrderIndex <= 5)
                    {
                        if (exitSignal)
                        {
                            var lastOrder = orders.LastOrDefault();
                            CheckAndAddCloseOrder(orders, lastOrder, date, close);
                        }
                        continue;
                    }

                    lastOrderIndex = i;

                    var order = new Order
                    {
                        Ticker = ticker,
                        Time = date,
                        Type = OrderType.Long,
                        Price = close,
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
                    && bearishMfi)
                {
                    // TODO: so that we don't have too many orders, we check if the last order is closed
                    if (i - lastOrderIndex <= 5)
                    {
                        if (exitSignal)
                        {
                            var lastOrder = orders.LastOrDefault();
                            CheckAndAddCloseOrder(orders, lastOrder, date, close);
                        }
                        continue;
                    }

                    lastOrderIndex = i;

                    var order = new Order
                    {
                        Ticker = ticker,
                        Time = date,
                        Type = OrderType.Short,
                        Price = close,
                        Quantity = 1,
                        Action = EnterSignal.Open,
                        Reason = $"Price cross below Kama"
                    };
                    AddIfLastOrderClose(orders, order);
                }

                if (exitSignal)
                {
                    var lastOrder = orders.LastOrDefault();
                    CheckAndAddCloseOrder(orders, lastOrder, date, close);
                }
            }

            return orders;
        }

        private static void CheckAndAddCloseOrder(List<Order> allOrderMade, Order? lastOrder, DateTime closeDate, decimal closePrice)
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
                    Reason = $"Exit previous {lastOrder.Type} postion"
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