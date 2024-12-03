namespace Stock.Trading.Models;

public class InHouseOpenPosition
{
    public InHouseOpenPosition(string ticker, decimal averagePrice, decimal quantity, DateTimeOffset entryTime, decimal? stopLossPrice = null, decimal? takeProfitPrice = null)
    {
        Ticker = ticker;
        AveragePrice = averagePrice;
        Quantity = quantity;
        EntryTime = entryTime;
        StopLossPrice = stopLossPrice;
        TakeProfitPrice = takeProfitPrice;
    }

    public decimal? TakeProfitPrice { get; set; }

    public decimal? StopLossPrice { get; }

    public string Ticker { get; }
    
    public decimal AveragePrice { get; }

    public decimal Quantity { get; }

    public DateTimeOffset EntryTime { get; }
    
    public bool TryClose(decimal exitPrice, DateTimeOffset exitTime, out InHouseClosedPosition closedPosition)
    {
        if (exitTime.Subtract(EntryTime).TotalMinutes == 0)
        {
            closedPosition = null;
            return false;
        }
        closedPosition = new InHouseClosedPosition(this, exitPrice, exitTime);
        return true;
    }
}