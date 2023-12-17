namespace Stock.Shared.Models
{
    public class Trade
    {
        public string Ticker { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public OrderAction Action { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public IList<Order> Orders { get; set; }
    }
}