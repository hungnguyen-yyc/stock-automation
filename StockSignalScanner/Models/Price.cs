namespace StockSignalScanner.Models
{
    public class HistoricalPrice : Price, IPrice
    {
        public decimal AdjClose { get; set; }
        public long UnadjustedVolume { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public decimal Vwap { get; set; }
        public string Label { get; set; }
        public decimal ChangeOverTime { get; set; }
    }

    public class Price : IPrice
    {
        public DateTimeOffset Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }

    public interface IPrice
    {
        public DateTimeOffset Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }
}
