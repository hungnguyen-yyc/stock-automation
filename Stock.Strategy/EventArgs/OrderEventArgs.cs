using Stock.Shared.Models;

namespace Stock.Strategies
{
    public class OrderEventArgs : EventArgs
    {
        public AutomateOrder Order { get; set; }

        public OrderEventArgs(AutomateOrder order)
        {
            Order = order;
        }
    }
}
