namespace Stock.Trading.Models;

public class InHouseClosedPosition
{
    public InHouseClosedPosition(InHouseOpenPosition openPosition, decimal exitPrice, DateTimeOffset exitTime)
    {
        OpenPosition = openPosition;
        ExitPrice = exitPrice;
        ExitTime = exitTime;
    }
    
    public InHouseOpenPosition OpenPosition { get; }

    public decimal ExitPrice { get; }
    
    public DateTimeOffset ExitTime { get; }
    
    public decimal Profit => (ExitPrice - OpenPosition.AveragePrice) * OpenPosition.Quantity;
}