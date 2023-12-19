using Skender.Stock.Indicators;

namespace Stock.Shared.Models
{
    public class PriceList : List<Price>
    {

    }

    public class Price : IPrice
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

        public NumericRange RangeBetweenBodyAndHigh
        {
            get
            {
                if (Close > Open)
                {
                    return new NumericRange(Close, High);
                }
                else
                {
                    return new NumericRange(Open, High);
                }
            }
        }

        public NumericRange RangeBetweenBodyAndLow
        {
            get
            {
                if (Close > Open)
                {
                    return new NumericRange(Open, Low);
                }
                else
                {
                    return new NumericRange(Close, Low);
                }
            }
        }

        public bool IsGreenCandle => Close > Open;

        public bool IsRedCandle => Close < Open;

        public bool IsDojiCandle => Close == Open;

        public NumericRange TopHalfOfCandle => new NumericRange((High + Low) / 2, High);

        public NumericRange BottomHalfOfCandle => new NumericRange(Low, (High + Low) / 2);

        public NumericRange CandleRange => new NumericRange(Low, High);

        public override bool Equals(object? obj)
        {
            if (obj is Price price)
            {
                return Date == price.Date
                    && Open == price.Open
                    && High == price.High
                    && Low == price.Low
                    && Close == price.Close
                    && Volume == price.Volume;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Date, Open, High, Low, Close, Volume);
        }

        public override string ToString()
        {
            return $"{Date:s};O:{Open};H:{High};L:{Low};C:{Close};V:{Volume}";
        }
    }

    public interface IPrice : IQuote
    {
    }
}
