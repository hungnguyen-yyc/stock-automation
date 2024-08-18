using Stock.Shared.Models;
using Stock.Strategies.Parameters;

namespace Stock.Strategies;

public interface ISwingPointStrategy
{
    event AlertEventHandler AlertCreated;
    event TrendLineEventHandler TrendLineCreated;
    event PivotLevelEventHandler PivotLevelCreated;

    void CheckForTopBottomTouch(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
    void CheckForTouchingDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
    void CheckForTouchingUpTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter);
}