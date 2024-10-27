using Stock.Shared.Models;

namespace Stock.Strategies.Parameters;

public class HmaEmaPriceStrategyParameter : IStrategyParameter
{
    public Timeframe Timeframe { get; set; }
}