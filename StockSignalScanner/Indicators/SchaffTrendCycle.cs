using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static decimal[] Divide(decimal[] numerator, decimal[] denominator)
        {
            decimal[] result = new decimal[numerator.Length];
            for (int i = 0; i < numerator.Length; i++)
            {
                if (denominator[i] == 0)
                {
                    result[i] = 0;
                }
                else
                {
                    result[i] = numerator[i] / denominator[i];
                }
            }
            return result;
        }


        // Function to sum an array over a given period
        public static decimal[] Sum(decimal[] input, int length)
        {
            decimal[] output = new decimal[input.Length];
            for (int i = length - 1; i < input.Length; i++)
            {
                decimal sum = 0;
                for (int j = 0; j < length; j++)
                {
                    sum += input[i - j];
                }
                output[i] = sum;
            }
            return output;
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

        // Multiplies each element in the array by a constant value and returns the result as a new array
        public static decimal[] Multiply(decimal constant, decimal[] array)
        {
            decimal[] result = new decimal[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = constant * array[i];
            }

            return result;
        }

        // Divides each element in the array by a constant value and returns the result as a new array
        public static decimal[] Divide(decimal[] array, decimal constant)
        {
            decimal[] result = new decimal[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[i] / constant;
            }

            return result;
        }


    }
}
