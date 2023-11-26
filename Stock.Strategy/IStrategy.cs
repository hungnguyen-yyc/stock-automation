using Stock.Shared.Models;
using Stock.Strategies.Parameters;

namespace Stock.Strategy
{
    public interface IStrategy
    {
        IList<Order> Run(string ticker, IStrategyParameter strategyParameter, Timeframe timeframe = Timeframe.Daily, int lastNDay1 = 5, int lastNDay2 = 3);
    }
}