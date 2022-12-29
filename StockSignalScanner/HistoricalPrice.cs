namespace TickerList
{
    public class HistoricalPrice
    {
        public string Ticker { get; set; }
        public IList<Price> Historical { get; set; }
    }
}
