using Skender.Stock.Indicators;

namespace StockSignalScanner.Models
{
    public class HistoricalPrice : Price, IPrice
    {
        public decimal AdjClose { get; set; }
        public long UnadjustedVolume { get; set; }
        public decimal Change { get; set; }
        public decimal? ChangePercent { get; set; }
        public decimal Vwap { get; set; }
        public string Label { get; set; }
        public decimal ChangeOverTime { get; set; }
    }

    public class Price : IPrice
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal Tema { get; set; }

        public Quote ToQuote()
        {
            return new Quote()
            {
                Volume = Volume,
                Close = Close,
                Open = Open,
                High = High,
                Low = Low,
                Date = Date,
            };
        }
    }

    public interface IPrice : IQuote
    {
        Quote ToQuote();
    }
}
