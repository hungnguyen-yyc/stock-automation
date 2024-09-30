using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;

namespace Stock.Strategy
{
    public interface IStrategy
    {
        string Description { get; }
        
        event AlertEventHandler AlertCreated;
    }
}