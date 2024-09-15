using Stock.Shared.Models;
using Stock.Strategies.Parameters;
using Stock.Strategy;

namespace Stock.Strategies;

public interface ISwingPointStrategy : IStrategy
{
    event TrendLineEventHandler TrendLineCreated;
    event PivotLevelEventHandler PivotLevelCreated;

    void CheckForTopBottomTouch(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
    void CheckForTouchingDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
    void CheckForTouchingUpTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
}