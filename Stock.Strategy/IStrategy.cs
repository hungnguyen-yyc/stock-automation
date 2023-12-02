using Stock.Shared.Models;
using Stock.Strategies.Parameters;

namespace Stock.Strategy
{
    public interface IStrategy
    {
        string Description { get; }

        Task<IList<Order>> RunBackTest(string ticker, IStrategyParameter strategyParameter, DateTime from, DateTime to, Timeframe timeframe = Timeframe.Daily);
    }
}