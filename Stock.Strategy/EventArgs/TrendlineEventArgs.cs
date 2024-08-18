using Stock.Shared.Models;

namespace Stock.Strategies.EventArgs;

public class TrendLineEventArgs : System.EventArgs
{
    public IReadOnlyList<TrendLine> TrendLines { get; }
    
    public TrendLineEventArgs(IReadOnlyList<TrendLine> trendLines)
    {
        TrendLines = trendLines;
    }
}

public class PivotLevelEventArgs : System.EventArgs
{
    public IReadOnlyList<PivotLevel> PivotLevels { get; }
    
    public PivotLevelEventArgs(IReadOnlyList<PivotLevel> pivotLevels)
    {
        PivotLevels = pivotLevels;
    }
}