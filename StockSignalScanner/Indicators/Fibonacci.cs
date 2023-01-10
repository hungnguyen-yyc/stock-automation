using StockSignalScanner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Indicators
{
    public enum RetracementLevel
    {
        Above100,
        Above61_8,
        Above50,
        Above38_2,
        Above23_6,
        Below23_6,
        Below38_2,
        Below50,
        Below61_8,
        Below100,
        AtRetracementLevel,
        UnableToDetect
    }

    public class FibonacciRetracement
    {
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public decimal Retracement100 { get; set; }
        public decimal Retracement61_8 { get; set; }
        public decimal Retracement50 { get; set; }
        public decimal Retracement38_2 { get; set; }
        public decimal Retracement23_6 { get; set; }
    }

    public class CurrentPriceFibonacciRetracementLevel
    {
        public FibonacciRetracement Retracement { get; set; }
        public RetracementLevel RetracementLevel { get; set; }
        public override string ToString()
        {
            return $"{RetracementLevel} | Low:{Retracement.Low} | High:{Retracement.High} | Retracement23_6:{Retracement.Retracement23_6} | Retracement38_2:{Retracement.Retracement38_2} " +
                $"| Retracement50:{Retracement.Retracement50} | Retracement61_8:{Retracement.Retracement61_8} | Retracement100:{Retracement.Retracement100}";
        }
    }

    internal class Fibonacci
    {
        public static CurrentPriceFibonacciRetracementLevel GetCurrentPriceState(IList<IPrice> prices, int startIndex, int endIndex)
        {
            // Check for valid input
            if (prices == null || prices.Count == 0 || startIndex < 0 || endIndex >= prices.Count || startIndex >= endIndex)
            {
                return null;
            }
            var currentIndex = prices.Count() - 1;
            var retracementLevel = RetracementLevel.UnableToDetect;

            // Calculate the Fibonacci retracement levels
            var fibo = SetFibonacciRetracementLevels(prices, startIndex, endIndex);

            // Check which retracement level the current price is above or below
            if (prices[currentIndex].Close > fibo.Retracement100)
            {
                retracementLevel = RetracementLevel.Above100;
            }
            else if (prices[currentIndex].Close > fibo.Retracement61_8)
            {
                retracementLevel = RetracementLevel.Above61_8;
            }
            else if (prices[currentIndex].Close > fibo.Retracement50)
            {
                retracementLevel = RetracementLevel.Above50;
            }
            else if (prices[currentIndex].Close > fibo.Retracement38_2)
            {
                retracementLevel = RetracementLevel.Above38_2;
            }
            else if (prices[currentIndex].Close > fibo.Retracement23_6)
            {
                retracementLevel = RetracementLevel.Above23_6;
            }
            else if (prices[currentIndex].Close < fibo.Retracement23_6)
            {
                retracementLevel = RetracementLevel.Below23_6;
            }
            else if (prices[currentIndex].Close < fibo.Retracement38_2)
            {
                retracementLevel = RetracementLevel.Below38_2;
            }
            else if (prices[currentIndex].Close < fibo.Retracement50)
            {
                retracementLevel = RetracementLevel.Below50;
            }
            else if (prices[currentIndex].Close < fibo.Retracement61_8)
            {
                retracementLevel = RetracementLevel.Below61_8;
            }
            else if (prices[currentIndex].Close < fibo.Retracement100)
            {
                retracementLevel = RetracementLevel.Below100;
            }
            else
            {
                retracementLevel = RetracementLevel.AtRetracementLevel;
            }

            return new CurrentPriceFibonacciRetracementLevel
            {
                RetracementLevel = retracementLevel,
                Retracement = fibo,
            };
        }

        private static FibonacciRetracement SetFibonacciRetracementLevels(IList<IPrice> prices, int startIndex, int endIndex)
        {
            // Check for valid input
            if (prices == null || prices.Count == 0 || startIndex < 0 || endIndex >= prices.Count || startIndex >= endIndex)
            {
                return new FibonacciRetracement
                {
                    High= 0,
                    Low= 0,
                    Retracement50= 0,
                    Retracement38_2= 0,
                    Retracement23_6= 0,
                    Retracement100= 0,
                    Retracement61_8= 0,
                };
            }

            // Calculate the high and low price over the given range
            decimal high = prices.Skip(endIndex - startIndex).Take(startIndex + 1).Max(x => x.High);
            decimal low = prices.Skip(endIndex - startIndex).Take(startIndex + 1).Min(x => x.Low);

            // Calculate the Fibonacci retracement levels
            decimal retracement23_6 = low + (high - low) * 23.6m / 100;
            decimal retracement38_2 = low + (high - low) * 38.2m / 100;
            decimal retracement50 = low + (high - low) * 50m / 100;
            decimal retracement61_8 = low + (high - low) * 61.8m / 100;
            decimal retracement100 = low + (high - low) * 100m / 100;

            return new FibonacciRetracement
            {
                High= high,
                Low= low,
                Retracement100= retracement100,
                Retracement23_6= retracement23_6,
                Retracement38_2= retracement38_2,
                Retracement50= retracement50,
                Retracement61_8= retracement61_8,
            };
        }
    }
}
