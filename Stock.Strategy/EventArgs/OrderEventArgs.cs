using Stock.Shared.Models;

namespace Stock.Strategies
{
    public class OrderEventArgs : EventArgs
    {
        public Order Order { get; set; }

        public OrderEventArgs(Order order)
        {
            Order = order;
        }
    }
}
