namespace StockSignalScanner.Models
{
    public class TickerHistoricalPrice
    {
        public string Symbol { get; set; }
        public IList<HistoricalPrice> Historical { get; set; }
    }

    public class TickerInterdayPrice
    {
        public IList<HistoricalPrice> Historical { get; set; }
    }
}
