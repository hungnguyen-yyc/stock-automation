using Skender.Stock.Indicators;

namespace Stock.Shared.Models
{
    public class Price : IPrice
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }

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
    }

    public interface IPrice : IQuote
    {
    }
}
