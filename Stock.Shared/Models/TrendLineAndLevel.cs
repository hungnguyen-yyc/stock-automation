namespace Stock.Shared.Models;

public class PivotLevel
{
    public Timeframe Timeframe { get; }
    
    public string Ticker { get; }
    
    public Price Level { get; }
    
    public int NumberOfSwingPointsIntersected { get; }
    
    public PivotLevel(Timeframe timeframe, string ticker, Price level, int numberOfSwingPointsIntersected)
    {
        Timeframe = timeframe;
        Ticker = ticker;
        Level = level;
        NumberOfSwingPointsIntersected = numberOfSwingPointsIntersected;
    }

    public override bool Equals(object? obj)
    {
        if (obj is PivotLevel pivotLevel)
        {
            return Timeframe == pivotLevel.Timeframe &&
                   Ticker == pivotLevel.Ticker &&
                   Level == pivotLevel.Level;
        }
        
        return false;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Timeframe, Ticker, Level);
    }
    
    public TrendLine ToTrendLine()
    {
        return new TrendLine(Timeframe, Ticker, Level, Level, NumberOfSwingPointsIntersected);
    }
}

public class TrendLine
{
    public Timeframe Timeframe { get; }
    public string Ticker { get; }
    public string StartPrice { get; }
    public string EndPrice { get; }
    
    public Price Start { get; }
    
    public Price End { get; }
    
    public int NumberOfSwingPointsIntersected { get; }

    public TrendLine(Timeframe timeframe, string ticker, Price start, Price end, int numberOfSwingPointsIntersected)
    {
        Timeframe = timeframe;
        Ticker = ticker;
        StartPrice = start.ToString();
        EndPrice = end.ToString();
        Start = start;
        End = end;
        NumberOfSwingPointsIntersected = numberOfSwingPointsIntersected;
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