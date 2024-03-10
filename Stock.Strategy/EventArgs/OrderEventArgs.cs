using Stock.Shared.Models;

namespace Stock.Strategies.EventArgs
{
    public class OrderEventArgs : System.EventArgs
    {
        public AutomateOrder Order { get; set; }

        public OrderEventArgs(AutomateOrder order)
        {
            Order = order;
        }
    }
}
