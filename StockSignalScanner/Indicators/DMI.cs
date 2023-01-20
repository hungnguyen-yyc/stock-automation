using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    internal class ADX
    {
        private static void ChangeInPlace(List<decimal> values)
        {
            for (int i = 0; i < values.Count - 1; i++)
            {
                values[i] = values[i + 1] - values[i];
            }
            values.RemoveAt(values.Count - 1);
        }

        private static List<decimal> TR(IList<IPrice> prices)
        {
            var tr = new List<decimal>(new decimal[prices.Count]);
            for (int i = 0; i < prices.Count; i++)
            {
                decimal currentRange = prices[i].High - prices[i].Low;
                decimal currentClose = prices[i].Close;
                decimal previousClose = (i == 0) ? 0 : prices[i - 1].Close;
                decimal closeGap = Math.Abs(currentClose - previousClose);
                tr[i] = Math.Max(currentRange, Math.Max(closeGap, (i == 0) ? 0 : prices[i].High - prices[i - 1].Low));
            }
            return tr;
        }

        private static List<decimal> RMA(IList<decimal> src, int length)
        {
            var alpha = 1.0m / length;
            var sum = new List<decimal>();
            for (int i = 0; i < src.Count; i++)
            {
                if (i < length - 1)
                {
                    sum.Add(src.Take(i + 1).Sum() / (i + 1));
                }
                else
                {
                    sum.Add(alpha * src[i] + (1 - alpha) * sum[i - 1]);
                }
            }
            return sum;
        }

        public static List<decimal> CalculateADX(IList<IPrice> prices, int len, int lensig)
        {
            // Calculate +DM, -DM
            List<decimal> plusDM = new List<decimal>(new decimal[prices.Count]);
            List<decimal> minusDM = new List<decimal>(new decimal[prices.Count]);
            for (int i = 0; i < prices.Count; i++)
            {
                if (i == 0)
                {
                    plusDM.Add(0);
                    minusDM.Add(0);
                    continue;
                }
                var up = prices[i].High - prices[i - 1].High;
                var down = -(prices[i].Low - prices[i - 1].Low);
                plusDM[i] = up > down && up > 0 ? up : 0;
                minusDM[i] = down > up && down > 0 ? down : 0;
            }

            // Calculate TR
            List<decimal> trur = TR(prices);

            // Calculate +DI, -DI, ADX
            List<decimal> diPlus = RMA(plusDM, len);
            List<decimal> diMinus = RMA(minusDM, len);
            List<decimal> adx = new List<decimal>(new decimal[prices.Count]);
            for (int i = 0; i < prices.Count; i++)
            {
                if (i < len - 1)
                {
                    diPlus[i] = 0;
                    diMinus[i] = 0;
                    adx[i] = 0;
                }
                else
                {
                    if (trur[i] != 0)
                    {
                        diPlus[i] = diPlus[i] * 100 / trur[i];
                        diMinus[i] = diMinus[i] * 100 / trur[i];
                        decimal sum = diPlus[i] + diMinus[i];
                        adx[i] = 100 * Math.Abs(diPlus[i] - diMinus[i]) / (sum == 0 ? 1 : sum);
                    }
                    else
                    {
                        diPlus[i] = 0;
                        diMinus[i] = 0;
                        adx[i] = 0;
                    }

                }
            }
            return RMA(adx, lensig);
        }
    }
}
