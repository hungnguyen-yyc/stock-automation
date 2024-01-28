namespace Stock.Shared.Models;

public class TrendLine
{
    public Timeframe Timeframe { get; }
    public string Ticker { get; }
    public string StartPrice { get; }
    public string EndPrice { get; }
    
    public bool IsLevel { get; }

    public TrendLine(Timeframe timeframe, string ticker, Price start, Price end)
    {
        Timeframe = timeframe;
        Ticker = ticker;
        StartPrice = start.ToString();
        EndPrice = end.ToString();
        IsLevel = start.Equals(end);
    }

    public override bool Equals(object? obj)
    {
        if (obj is TrendLine trendLine)
        {
            return Timeframe == trendLine.Timeframe &&
                   Ticker == trendLine.Ticker &&
                   StartPrice == trendLine.StartPrice &&
                   EndPrice == trendLine.EndPrice;
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Timeframe, Ticker, EndPrice, EndPrice);
    }
}