using Stock.Shared.Models;

namespace Stock.Strategies.Parameters
{
    public interface IStrategyParameter
    {
        public Timeframe Timeframe { get; set; }
    }
}
