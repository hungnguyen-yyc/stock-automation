using StockSignalScanner.Models;

namespace StockSignalScanner.Indicators
{
    internal class SchaffTrendCycle
    {
        public static decimal[] CalculateSTC(IList<IPrice> prices, int maShort, int maLong, int cycle, decimal factor = 0.5m)
        {
            var closePrices = prices.Select(p => p.Close).ToArray();
            var maShortEMA = MovingAverage.CalculateEMA(closePrices.ToList(), maShort);
            var maLongEMA = MovingAverage.CalculateEMA(closePrices.ToList(), maLong);

            var macd = Subtract(maShortEMA.ToArray(), maLongEMA.ToArray());
            var st = new decimal[closePrices.Length];
            var st2 = new decimal[closePrices.Length];

            for (int i = cycle + 1; i < closePrices.Length; i++)
            {
                decimal llv = 0;
                decimal hhv = 0;
                for (int j = i; j > i - cycle; j--)
                {
                    if (j == i)
                    {
                        llv = macd[j];
                        hhv = macd[j];
                    }
                    else
                    {
                        llv = Math.Min(llv, macd[j]);
                        hhv = Math.Max(hhv, macd[j]);
                    }
                }

                decimal stoch1 = 0;
                if (hhv - llv != 0)
                    stoch1 = ((macd[i] - llv) / (hhv - llv)) * 100;
                else
                    stoch1 = stoch1 = st[i - 1];

                if (i - 1 >= 0 && i - 1 < st.Length)
                    st[i] = factor * (stoch1 - st[i - 1]) + st[i - 1];

                llv = 0;
                hhv = 0;
                for (int j = i; j > i - cycle; j--)
                {
                    if (j == i)
                    {
                        llv = st[j];
                        hhv = st[j];
                    }
                    else
                    {
                        llv = Math.Min(llv, st[j]);
                        hhv = Math.Max(hhv, st[j]);
                    }
                }

                decimal stoch2 = 0;
                if (hhv - llv != 0)
                    stoch2 = ((st[i] - llv) / (hhv - llv)) * 100;
                else
                    stoch2 = stoch2 = st2[i - 1];

                if (i - 1 >= 0 && i - 1 < st2.Length)
                    st2[i] = factor * (stoch2 - st2[i - 1]) + st2[i - 1];
            }

            return st2;
        }

        // Subtracts array2 from array1 element by element and returns the result as a new array
        public static decimal[] Subtract(decimal[] array1, decimal[] array2)
        {
            if (array1.Length != array2.Length)
            {
                throw new ArgumentException("Array lengths must be equal");
            }

            decimal[] result = new decimal[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = array1[i] - array2[i];
            }

            return result;
        }

    }
}
