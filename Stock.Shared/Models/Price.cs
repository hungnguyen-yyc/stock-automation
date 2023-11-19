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
    }

    public interface IPrice : IQuote
    {
    }
}
