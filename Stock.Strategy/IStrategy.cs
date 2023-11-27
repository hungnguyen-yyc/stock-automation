using Stock.Shared.Models;
using Stock.Strategies.Parameters;

namespace Stock.Strategy
{
    public interface IStrategy
    {
        string Description { get; }

        IList<Order> Run(string ticker, IStrategyParameter strategyParameter, DateTime from, Timeframe timeframe = Timeframe.Daily);
    }
}