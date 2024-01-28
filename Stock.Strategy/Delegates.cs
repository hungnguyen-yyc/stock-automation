using Stock.Strategies.EventArgs;

namespace Stock.Strategies
{
    public delegate void OrderEventHandler(object sender, OrderEventArgs e);
    public delegate void AlertEventHandler(object sender, AlertEventArgs e);
    public delegate void TrendLineEventHandler(object sender, TrendLineEventArgs e);
}
