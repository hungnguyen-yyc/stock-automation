using Stock.Shared.Models;

namespace Stock.Strategies.Parameters;

public class ImmediateSwingLowParameterProvider
{
    private static readonly ImmediateSwingLowEntryParameter DefaultEntryParameter = new ImmediateSwingLowEntryParameter
    {
        TemaPeriod = 150,
        NumberOfCandlesticksToLookBack = 10,
        Timeframe = Timeframe.Hour1
    };
    
    private static readonly ImmediateSwingLowExitParameter DefaultExitParameter = new ImmediateSwingLowExitParameter
    {
        TemaPeriod = 200,
        NumberOfCandlesticksToLookBack = 10,
        Timeframe = Timeframe.Hour1,
        StopLoss = 0,
        TakeProfit = 0
    };
    
    public static ImmediateSwingLowEntryParameter GetEntryParameter(string ticker)
    {
        return DefaultEntryParameter;
    }
}

public class ImmediateSwingLowExitParameter : ImmediateSwingLowEntryParameter
{
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
}

public class ImmediateSwingLowEntryParameter : IStrategyParameter
{
    public int TemaPeriod { get; set; }
    public int NumberOfCandlesticksToLookBack { get; set; }
    public Timeframe Timeframe { get; set; }
}