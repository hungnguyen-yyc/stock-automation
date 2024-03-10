namespace Stock.Shared.Models
{
    public enum OrderPosition
    {
        Long,
        Short,
    }

    public enum OrderAction
    {
        BUY,
        SELL,
    }

    public enum  OrderType
    {
        MKT,
        LMT,
    }

    public enum Currency
    {
        USD,
        EUR,
        GBP,
        CAD
    }

    public enum SecType
    {
        OPT,
        STK
    }
}