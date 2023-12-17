namespace Stock.Strategies
{
    public delegate void OrderEventHandler(object sender, OrderEventArgs e);
    public delegate void AlertEventHandler(object sender, AlertEventArgs e);
}
