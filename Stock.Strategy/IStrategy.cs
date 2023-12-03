using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;

namespace Stock.Strategy
{
    public interface IStrategy
    {
        string Description { get; }

        public event OrderEventHandler OrderCreated;

        IList<Order> Run(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
    }
}