using StockSignalScanner.Indicators;
using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Strategies
{
    internal class EMACrossingStrategy
    {
        public static void RunEMACross50200WithAdxStrategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-50-200-adx-gt-25", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema50Cross200 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 50, 200);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema50Cross200.Count; i++)
            {
                (DateTime, CrossDirection) ema = ema50Cross200[i];
                var adx = data.GetADXAtDate(ema.Item1);
                if (adx > 25)
                {
                    totalCrosses += 1;
                    if (ema.Item2 == CrossDirection.CROSS_ABOVE)
                    {
                        var prices = data.GetPricesInPeriod(ema.Item1, pricesAfterNDays);
                        var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                        var pricesString = string.Join(",", prices);
                        WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{ema.Item1.ToString("yyyy-MM-dd")},{pricesString},{adx}");
                        if (hasAnyExpectedPrices)
                        {
                            success += 1;
                        }
                    }
                    else if (ema.Item2 == CrossDirection.CROSS_BELOW)
                    {
                        var prices = data.GetPricesInPeriod(ema.Item1, pricesAfterNDays);
                        var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                        var pricesString = string.Join(",", prices);
                        WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{ema.Item1.ToString("yyyy-MM-dd")},{pricesString},{adx}");
                        if (hasAnyExpectedPrices)
                        {
                            success += 1;
                        }
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross50200Strategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-50-200", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema50Cross200 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 50, 200);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema50Cross200.Count; i++)
            {
                (DateTime, CrossDirection) ema = ema50Cross200[i];
                totalCrosses += 1;
                if (ema.Item2 == CrossDirection.CROSS_ABOVE)
                {
                    var prices = data.GetPricesInPeriod(ema.Item1, pricesAfterNDays);
                    var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                    var pricesString = string.Join(",", prices);
                    WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{ema.Item1.ToString("yyyy-MM-dd")},{pricesString}");
                    if (hasAnyExpectedPrices)
                    {
                        success += 1;
                    }
                }
                else if (ema.Item2 == CrossDirection.CROSS_BELOW)
                {
                    var prices = data.GetPricesInPeriod(ema.Item1, pricesAfterNDays);
                    var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                    var pricesString = string.Join(",", prices);
                    WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{ema.Item1.ToString("yyyy-MM-dd")},{pricesString}");
                    if (hasAnyExpectedPrices)
                    {
                        success += 1;
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross1334WithAdxStrategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-13-34-adx-gt-25", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema13Cross34 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 13, 34);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema13Cross34.Count; i++)
            {
                (DateTime, CrossDirection) ema13 = ema13Cross34[i];
                var adx = data.GetADXAtDate(ema13.Item1);
                if (adx > 25)
                {
                    totalCrosses += 1;
                    if (ema13.Item2 == CrossDirection.CROSS_ABOVE)
                    {
                        var prices = data.GetPricesInPeriod(ema13.Item1, pricesAfterNDays);
                        var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                        var pricesString = string.Join(",", prices);
                        WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{ema13.Item1.ToString("yyyy-MM-dd")},{pricesString},{adx}");
                        if (hasAnyExpectedPrices)
                        {
                            success += 1;
                        }
                    }
                    else if (ema13.Item2 == CrossDirection.CROSS_BELOW)
                    {
                        var prices = data.GetPricesInPeriod(ema13.Item1, pricesAfterNDays);
                        var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                        var pricesString = string.Join(",", prices);
                        WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{ema13.Item1.ToString("yyyy-MM-dd")},{pricesString},{adx}");
                        if (hasAnyExpectedPrices)
                        {
                            success += 1;
                        }
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross1334Strategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-13-34", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema13Cross34 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 13, 34);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema13Cross34.Count; i++)
            {
                (DateTime, CrossDirection) ema13 = ema13Cross34[i];
                totalCrosses += 1;
                if (ema13.Item2 == CrossDirection.CROSS_ABOVE)
                {
                    var prices = data.GetPricesInPeriod(ema13.Item1, pricesAfterNDays);
                    var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                    var pricesString = string.Join(",", prices);
                    WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{ema13.Item1.ToString("yyyy-MM-dd")},{pricesString}");
                    if (hasAnyExpectedPrices)
                    {
                        success += 1;
                    }
                }
                else if (ema13.Item2 == CrossDirection.CROSS_BELOW)
                {
                    var prices = data.GetPricesInPeriod(ema13.Item1, pricesAfterNDays);
                    var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                    var pricesString = string.Join(",", prices);
                    WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{ema13.Item1.ToString("yyyy-MM-dd")},{pricesString}");
                    if (hasAnyExpectedPrices)
                    {
                        success += 1;
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross21345589WithAdxStrategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-21-34-55-89-adx-gt-25", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema21Cross89 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 21, 89);
            var ema34Cross89 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 34, 89);
            var ema55Cross89 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 55, 89);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema21Cross89.Count; i++)
            {
                (DateTime, CrossDirection) ema21 = ema21Cross89[i];
                for (int j = 0; j < ema34Cross89.Count; j++)
                {
                    (DateTime, CrossDirection) ema34 = ema34Cross89[j];
                    for (int k = 0; k < ema55Cross89.Count; k++)
                    {
                        (DateTime, CrossDirection) ema55 = ema55Cross89[k];
                        var dates = new List<DateTime>() { ema21.Item1, ema34.Item1, ema55.Item1 };
                        dates = dates.OrderBy(x => x).ToList();
                        var adx0 = data.GetADXAtDate(dates[0].Date);
                        var adx1 = data.GetADXAtDate(dates[1].Date);
                        var adx2 = data.GetADXAtDate(dates[2].Date);
                        var adx = Math.Max(adx2, Math.Max(adx1, adx0));
                        if (dates[2].Date.Subtract(dates[0]).TotalDays <= crossInDays && adx > 25)
                        {
                            totalCrosses += 1;
                            if (ema21.Item2 == CrossDirection.CROSS_ABOVE
                                && ema34.Item2 == CrossDirection.CROSS_ABOVE
                                && ema55.Item2 == CrossDirection.CROSS_ABOVE)
                            {
                                var prices = data.GetPricesInPeriod(dates[2], pricesAfterNDays);
                                var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                                var pricesString = string.Join(",", prices);
                                WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{dates[2].ToString("yyyy-MM-dd")},{pricesString},{adx}");
                                if (hasAnyExpectedPrices)
                                {
                                    success += 1;
                                }
                            }else if (ema21.Item2 == CrossDirection.CROSS_BELOW
                                && ema34.Item2 == CrossDirection.CROSS_BELOW
                                && ema55.Item2 == CrossDirection.CROSS_BELOW)
                            {
                                var prices = data.GetPricesInPeriod(dates[2], pricesAfterNDays);
                                var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                                var pricesString = string.Join(",", prices);
                                WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{dates[2].ToString("yyyy-MM-dd")},{pricesString},{adx}");
                                if (hasAnyExpectedPrices)
                                {
                                    success += 1;
                                }
                            }
                        }
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross21345589Strategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-21-34-55-89", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema21Cross89 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 21, 89);
            var ema34Cross89 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 34, 89);
            var ema55Cross89 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 55, 89);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema21Cross89.Count; i++)
            {
                (DateTime, CrossDirection) ema21 = ema21Cross89[i];
                for (int j = 0; j < ema34Cross89.Count; j++)
                {
                    (DateTime, CrossDirection) ema34 = ema34Cross89[j];
                    for (int k = 0; k < ema55Cross89.Count; k++)
                    {
                        (DateTime, CrossDirection) ema55 = ema55Cross89[k];
                        var dates = new List<DateTime>() { ema21.Item1, ema34.Item1, ema55.Item1 };
                        dates = dates.OrderBy(x => x).ToList();
                        if (dates[2].Date.Subtract(dates[0]).TotalDays <= crossInDays)
                        {
                            totalCrosses += 1;
                            if (ema21.Item2 == CrossDirection.CROSS_ABOVE
                                && ema34.Item2 == CrossDirection.CROSS_ABOVE
                                && ema55.Item2 == CrossDirection.CROSS_ABOVE)
                            {
                                var prices = data.GetPricesInPeriod(dates[1], pricesAfterNDays);
                                var hasAnyExpectedPrices = prices.Skip(2).Any(i => i > prices[0]);
                                var pricesString = string.Join(",", prices);
                                WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{dates[1].ToString("yyyy-MM-dd")},{pricesString}");
                                if (hasAnyExpectedPrices)
                                {
                                    success += 1;
                                }
                            }
                            else if (ema21.Item2 == CrossDirection.CROSS_BELOW
                                && ema34.Item2 == CrossDirection.CROSS_BELOW
                                && ema55.Item2 == CrossDirection.CROSS_BELOW)
                            {
                                var prices = data.GetPricesInPeriod(dates[2], pricesAfterNDays);
                                var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                                var pricesString = string.Join(",", prices);
                                WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{dates[1].ToString("yyyy-MM-dd")},{pricesString}");
                                if (hasAnyExpectedPrices)
                                {
                                    success += 1;
                                }
                            }
                        }
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross213455WithAdxStrategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-21-34-55-adx-gt-25", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema21Cross55 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 21, 55);
            var ema34Cross55 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 34, 55);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema21Cross55.Count; i++)
            {
                (DateTime, CrossDirection) ema21 = ema21Cross55[i];
                for (int j = 0; j < ema34Cross55.Count; j++)
                {
                    (DateTime, CrossDirection) ema34 = ema34Cross55[j];
                    var dates = new List<DateTime>() { ema21.Item1, ema34.Item1 };
                    dates = dates.OrderBy(x => x).ToList();
                    var adx0 = data.GetADXAtDate(dates[0].Date);
                    var adx1 = data.GetADXAtDate(dates[1].Date);
                    var adx = Math.Max(adx1, adx0);
                    if (dates[1].Date.Subtract(dates[0]).TotalDays <= crossInDays && adx > 25)
                    {
                        totalCrosses += 1;
                        if (ema21.Item2 == CrossDirection.CROSS_ABOVE
                            && ema34.Item2 == CrossDirection.CROSS_ABOVE)
                        {
                            var prices = data.GetPricesInPeriod(dates[1], pricesAfterNDays);
                            var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                            var pricesString = string.Join(",", prices);
                            WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{dates[1].ToString("yyyy-MM-dd")},{pricesString},{adx}");
                            if (hasAnyExpectedPrices)
                            {
                                success += 1;
                            }
                        }
                        else if (ema21.Item2 == CrossDirection.CROSS_BELOW
                            && ema34.Item2 == CrossDirection.CROSS_BELOW)
                        {
                            var prices = data.GetPricesInPeriod(dates[1], pricesAfterNDays);
                            var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                            var pricesString = string.Join(",", prices);
                            WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{dates[1].ToString("yyyy-MM-dd")},{pricesString},{adx}");
                            if (hasAnyExpectedPrices)
                            {
                                success += 1;
                            }
                        }
                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        public static void RunEMACross213455Strategy(StockDataAggregator data, int crossInDays, int pricesAfterNDays)
        {
            string pathStockStrategy, pathFileNameStock, pathStock;
            CreateStrategyDirectory($"ema-cross-21-34-55", data, crossInDays, pricesAfterNDays, out pathStockStrategy, out pathFileNameStock, out pathStock);

            var ema21Cross55 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 21, 55);
            var ema34Cross55 = data.CheckAllEMACrossInLastNDays(data.NumberOfTradingDays, 34, 55);
            var totalCrosses = 0;
            var success = 0;

            for (int i = 0; i < ema21Cross55.Count; i++)
            {
                (DateTime, CrossDirection) ema21 = ema21Cross55[i];
                for (int j = 0; j < ema34Cross55.Count; j++)
                {
                    (DateTime, CrossDirection) ema34 = ema34Cross55[j];
                    var dates = new List<DateTime>() { ema21.Item1, ema34.Item1 };
                    dates = dates.OrderBy(x => x).ToList();
                    if (dates[1].Date.Subtract(dates[0]).TotalDays <= crossInDays)
                    {
                        totalCrosses += 1;
                        if (ema21.Item2 == CrossDirection.CROSS_ABOVE
                            && ema34.Item2 == CrossDirection.CROSS_ABOVE)
                        {
                            var prices = data.GetPricesInPeriod(dates[1], pricesAfterNDays);
                            var hasAnyExpectedPrices = prices.Skip(1).Any(i => i > prices[0]);
                            var pricesString = string.Join(",", prices);
                            WriteToFile(pathStock, $"{CrossDirection.CROSS_ABOVE},{dates[1].ToString("yyyy-MM-dd")},{pricesString}");
                            if (hasAnyExpectedPrices)
                            {
                                success += 1;
                            }
                        }
                        else if (ema21.Item2 == CrossDirection.CROSS_BELOW
                            && ema34.Item2 == CrossDirection.CROSS_BELOW)
                        {
                            var prices = data.GetPricesInPeriod(dates[1], pricesAfterNDays);
                            var hasAnyExpectedPrices = prices.Skip(1).Any(i => i < prices[0]);
                            var pricesString = string.Join(",", prices);
                            WriteToFile(pathStock, $"{CrossDirection.CROSS_BELOW},{dates[1].ToString("yyyy-MM-dd")},{pricesString}");
                            if (hasAnyExpectedPrices)
                            {
                                success += 1;
                            }
                        }

                    }
                }
            }

            UpdateFilenameWithResult(pathStockStrategy, pathFileNameStock, pathStock, totalCrosses, success);
        }

        private static void CreateStrategyDirectory(string name, StockDataAggregator data, int crossInDays, int pricesAfterNDays, out string pathStockStrategy, out string pathFileNameStock, out string pathStock)
        {
            pathStockStrategy = Path.Combine($"C:\\Users\\hnguyen\\Documents\\stock-scan-logs", $"strategy", name);
            if (!Directory.Exists(pathStockStrategy))
            {
                Directory.CreateDirectory(pathStockStrategy);
            }
            pathFileNameStock = $"{data.Symbol}_{data.ExchangeShortName}_{crossInDays}_{pricesAfterNDays}";
            pathStock = Path.Combine(pathStockStrategy, $"{pathFileNameStock}.csv");
            if (File.Exists(pathStock))
            {
                File.Delete(pathStock);
            }
        }

        private static void UpdateFilenameWithResult(string pathStockStrategy, string pathFileNameStock, string pathStock, int totalCrosses, int success)
        {
            var newFileName = Path.GetFileNameWithoutExtension(pathFileNameStock);
            if (totalCrosses == 0)
            {
                newFileName += $"_no_cross";
            }
            else
            {
                newFileName += $"_crosses_{totalCrosses}_success_{success}_rate_{success * 100 / totalCrosses}";
            }
            var newFile = Path.Combine(pathStockStrategy, $"{newFileName}.csv");
            if (File.Exists(newFile))
            {
                File.Delete(newFile);
            }
            if (File.Exists(pathStock))
            {
                File.Move(pathStock, newFile);
            }
        }

        private static void WriteToFile(string filePath, string content)
        {
            if (!File.Exists(filePath))
            {
                File.AppendAllLines(filePath, new[] { content });
                return;
            }
            File.AppendAllLines(filePath, new[] { content });
        }
    }
}
