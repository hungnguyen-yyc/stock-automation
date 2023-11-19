using Skender.Stock.Indicators;
using Stock.Shared.Helpers;
using Stock.Shared.Models;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using System.Diagnostics;

namespace Stock.Strategy
{
    public class TemaSarStochRsiKeltnerChannelStrategy
    {
        //public static IList<Order> Run(string ticker, KamaSarMfiParameter param, double gap = 2.5, int lastNDay = 3)
        //{
        //    var stockData = new StockDataCollector();
        //    var prices = stockData.CollectData(ticker, Timeframe.Daily, DateTime.Now.AddYears(-4)).Result;

        //    if (prices == null || prices.Count < 155)
        //    {
        //        return new List<Order>();
        //    }

        //    prices = prices.Reverse().ToList();

        //    var orders = new List<Order>();
        //    var tema = prices.GetTema(100).ToArray();
        //    var keltnerChannels = prices.GetKeltner(param.KeltnerPeriod, param.KeltnerMultiplier, param.KeltnerAtrPeriod).ToArray();
        //    var sar = prices.GetParabolicSar(param.SarAcceleration, param.SarMaxAcceleration, param.SarInitial).ToArray();
        //    var stochRsi = prices.GetStochRsi(param.RsiPeriod, param.StochPeriod, param.StochRsiSignalPeriod, param.StochRsiSmoothPeriod).ToArray();

        //    var lastOrderIndex = 0;
        //    for (var i = 155; i < prices.Count; i++)
        //    {
        //        var price = prices[i];
        //        var date = price.Date;
        //        var close = price.Close;
        //        var temaValue = tema[i].Tema;
        //        var keltnerUpperBand = keltnerChannels[i].UpperBand;
        //        var keltnerLowerBand = keltnerChannels[i].LowerBand;
        //        var sarValue = sar[i].Sar;
        //        var stochRsiValue = stochRsi[i].StochRsi;
        //        var stochRsiSignal = stochRsi[i].Signal;

        //        var lastNDayClose = prices.Skip(i - lastNDay).Take(lastNDay).Select(x => x.Close).ToList();
        //        var lastNDayTema = tema.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)x.Tema!).ToList();
        //        var lastNDayStochRsi = stochRsi.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)x.StochRsi!).ToList();
        //        var upperRange80 = prices.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)80).ToList();
        //        var lowerRange20 = prices.Skip(i - lastNDay).Take(lastNDay).Select(x => (decimal)20).ToList();

        //        var sarLowerThanPrice = (decimal)sarValue! < close;
        //        var sarGreaterThanPrice = (decimal)sarValue! > close;
        //        var sarReverseLastNDay = sar.Skip(i - lastNDay).Take(lastNDay).Any(x => x.IsReversal ?? false);
        //        var stochRsiCross80 = CrossDirectionDetector.GetCrossDirection(lastNDayStochRsi, upperRange80);
        //        var stochRsiCross20 = CrossDirectionDetector.GetCrossDirection(lastNDayStochRsi, lowerRange20);
        //        var priceCrossTema = CrossDirectionDetector.GetCrossDirection(lastNDayClose, lastNDayTema);
        //        var priceLowerThanKelnerUpper = close < (decimal)keltnerUpperBand!;
        //        var priceHigherThanKelnerLower = close > (decimal)keltnerLowerBand!;

        //        /**
        //         * check for touching tema from below or above
        //         * we check for the last 5 days (for example) under tema or above tema
        //         * and we check if last 2 days are consolidating
        //         */
        //        var currentPriceCompletelyAboveTema = (double)price.Open >= temaValue && (double)price.Low >= temaValue;
        //        var currentPriceCompletelyUnderTema = (double)price.Open <= temaValue && (double)price.High <= temaValue;
        //        var secondLastPriceTouchTema = (double)price.Low <= tema[i - 1].Tema && (double)price.High >= tema[i - 1].Tema;
        //        var lastNPriceUnderOrTouchTema = prices
        //            .Skip(i - lastNDay - 1).Take(lastNDay)
        //            .Zip(tema.Skip(i - lastNDay - 1)).Take(lastNDay)
        //            .All(x =>
        //                (double)x.First.Close <= x.Second.Tema
        //                || (double)x.First.Low <= x.Second.Tema
        //                || (double)x.First.High <= x.Second.Tema
        //                || (double)x.First.Open <= x.Second.Tema);
        //        var lastNPriceAboveOrTouchTema = prices
        //            .Skip(i - lastNDay - 1).Take(lastNDay)
        //            .Zip(tema.Skip(i - lastNDay - 1)).Take(lastNDay)
        //            .All(x =>
        //                (double)x.First.Close >= x.Second.Tema
        //                || (double)x.First.Low >= x.Second.Tema
        //                || (double)x.First.High >= x.Second.Tema
        //                || (double)x.First.Open >= x.Second.Tema);

        //        if (priceCrossTema == CrossDirection.CROSS_ABOVE && stochRsiCross20 == CrossDirection.CROSS_ABOVE && sarReverseLastNDay && sarLowerThanPrice)
        //        {
        //            // TODO: so that we don't have too many orders, we check if the last order is closed
        //            if (i - lastOrderIndex <= 5)
        //            {
        //                continue;
        //            }

        //            lastOrderIndex = i;

        //            var order = new Order
        //            {
        //                Ticker = ticker,
        //                Time = date,
        //                Type = OrderType.Long,
        //                Price = close,
        //                Quantity = 1,
        //                Action = EnterSignal.Open
        //            };
        //            AddIfLastOrderClose(orders, order);
        //            Debug.WriteLine($"Cross Long: {date:yyyy-MM-dd hh:mm:ss}");
        //        }
        //        else if (priceCrossTema == CrossDirection.CROSS_BELOW && stochRsiCross80 == CrossDirection.CROSS_BELOW && sarReverseLastNDay && sarGreaterThanPrice)
        //        {
        //            // TODO: so that we don't have too many orders, we check if the last order is closed
        //            if (i - lastOrderIndex <= 5)
        //            {
        //                continue;
        //            }

        //            lastOrderIndex = i;

        //            var order = new Order
        //            {
        //                Ticker = ticker,
        //                Time = date,
        //                Type = OrderType.Short,
        //                Price = close,
        //                Quantity = 1,
        //                Action = EnterSignal.Open
        //            };
        //            AddIfLastOrderClose(orders, order);
        //            Debug.WriteLine($"Cross Short: {date:yyyy-MM-dd hh:mm:ss}");
        //        }
        //        else if (lastNPriceAboveOrTouchTema && currentPriceCompletelyAboveTema && secondLastPriceTouchTema && sarReverseLastNDay && sarLowerThanPrice)
        //        {
        //            // TODO: so that we don't have too many orders, we check if the last order is closed
        //            if (i - lastOrderIndex <= 5)
        //            {
        //                continue;
        //            }

        //            lastOrderIndex = i;

        //            var order = new Order
        //            {
        //                Ticker = ticker,
        //                Time = date,
        //                Type = OrderType.Long,
        //                Price = close,
        //                Quantity = 1,
        //                Action = EnterSignal.Open
        //            };
        //            AddIfLastOrderClose(orders, order);
        //            Debug.WriteLine($"Touch Long: {date:yyyy-MM-dd hh:mm:ss}");
        //        }
        //        else if (lastNPriceUnderOrTouchTema && currentPriceCompletelyUnderTema && secondLastPriceTouchTema && sarReverseLastNDay && sarGreaterThanPrice)
        //        {
        //            // TODO: so that we don't have too many orders, we check if the last order is closed
        //            if (i - lastOrderIndex <= 5)
        //            {
        //                continue;
        //            }

        //            lastOrderIndex = i;

        //            var order = new Order
        //            {
        //                Ticker = ticker,
        //                Time = date,
        //                Type = OrderType.Short,
        //                Price = close,
        //                Quantity = 1,
        //                Action = EnterSignal.Open
        //            };
        //            AddIfLastOrderClose(orders, order);
        //            Debug.WriteLine($"Touch Short: {date:yyyy-MM-dd hh:mm:ss}");
        //        }

        //    }

        //    return orders;
        //}

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