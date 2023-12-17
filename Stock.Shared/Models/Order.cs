namespace Stock.Shared.Models
{
    public class Order
    {
        public OrderType Type { get; set; }
        public DateTime Time { get; set; }
        public OrderAction Action { get; set; }
        public decimal Quantity { get; set; }
        public string Ticker { get; set; }
        public string Reason { get; set; }
        public IPrice Price { get; set; }

        public override string ToString()
        {
            return $"{Time:s};{Ticker};{Action};{Type};{Price.Close};{Reason}";
        }
    }
}