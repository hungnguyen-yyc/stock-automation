namespace StockSignalScanner.Models
{
    public class TickerHistoricalPrice
    {
        public string Ticker { get; set; }
        public IList<HistoricalPrice> Historical { get; set; }
    }
}
