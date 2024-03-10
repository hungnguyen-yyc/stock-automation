using Stock.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Shared.Extensions
{
    public static class PriceListExtensions
    {
        public static Line GetPriceBodyLineCoordinate(this List<Price> list, Price price)
        {
            var index = list.IndexOf(price);

            if (index < 0)
                throw new Exception("Price not found in list");

            var high = new Point(index, price.Open);
            var low = new Point(index, price.Close);

            return new Line(low, high);
        }

        // get line from the start (open or close) of the body to the end of the candle (low)
        public static Line GetPriceBodyStartToLowLineCoordinate(this List<Price> list, Price price)
        {
            var index = list.IndexOf(price);

            if (index < 0)
                throw new Exception("Price not found in list");

            var high = price.Close > price.Open ? new Point(index, price.Close) : new Point(index, price.Open);

            var low = new Point(index, price.Low);

            return new Line(low, high);
        }

        // get line from the start (open or close) of the body to the end of the candle (high)
        public static Line GetPriceBodyStartToHighLineCoordinate(this List<Price> list, Price price)
        {
            var index = list.IndexOf(price);

            if (index < 0)
                throw new Exception("Price not found in list");

            var high = price.Close > price.Open ? new Point(index, price.Open) : new Point(index, price.Close);

            var low = new Point(index, price.High);

            return new Line(low, high);
        }

        public static Line GetPriceHighLineCoordinate(this List<Price> list, Price price)
        {
            var index = list.IndexOf(price);

            if (index < 0)
                throw new Exception("Price not found in list");

            var topRange = price.RangeBetweenBodyAndHigh;

            return new Line(new Point(index, topRange.Low), new Point(index, topRange.High));
        }

        public static Line GetPriceLowLineCoordinate(this List<Price> list, Price price)
        {
            var index = list.IndexOf(price);

            if (index < 0)
                throw new Exception("Price not found in list");

            var bottomRange = price.RangeBetweenBodyAndLow;

            return new Line(new Point(index, bottomRange.Low), new Point(index, bottomRange.High));
        }

        public static NumericRange GetHighLowOfPriceList(this List<Price> list)
        {
            var high = list.Max(x => x.High);
            var low = list.Min(x => x.Low);

            return new NumericRange(low, high);
        }
    }
}
