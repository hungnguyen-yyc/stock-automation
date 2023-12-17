using Stock.Shared.Models;
using Stock.Strategies.Helpers;
using Stock.Strategies.Parameters;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies
{
    public class SwingPointsLiveTradingStrategy
    {
        public event AlertEventHandler AlertCreated;

        public string Description => "This strategy looks back a number of candles (specified in parameters) and calculates swing highs and lows. \n"
            + "The order then will be created at 2 candles after most recent swing lows or highs found. \n"
            + "The problem now is how to eliminate loss as soon as posible.";

        public void CheckForBreakAboveDownTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var highLines = SwingPointAnalyzer.GetTrendlines(ascSortedByDatePrice, numberOfCandlesticksToLookBack, true);
            var trendingDownLines = highLines.Where(x => x.Item2.High < x.Item1.High).ToList();

            var secondLastPrice = ascSortedByDatePrice[ascSortedByDatePrice.Count - 2];
            var price = ascSortedByDatePrice.Last();

            foreach (var line in trendingDownLines)
            {
                var lineStart = line.Item1;
                var lineEnd = line.Item2;
                var lineStartIndex = ascSortedByDatePrice.IndexOf(lineStart);
                var lineEndIndex = ascSortedByDatePrice.IndexOf(lineEnd);
                var lineStartPoint = new Point(lineStartIndex, lineStart.High);
                var lineEndPoint = new Point(lineEndIndex, lineEnd.High);
                var secondLastPointIndex = ascSortedByDatePrice.IndexOf(secondLastPrice);
                var secondLastHighPoint = new Point(secondLastPointIndex, secondLastPrice.High);
                var secondLastLowPoint = new Point(secondLastPointIndex, secondLastPrice.Low);

                if (SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, lineEndPoint, secondLastLowPoint, secondLastHighPoint))
                {
                    if (price.Close > secondLastPrice.Close)
                    {
                        var alert = new Alert
                        {
                            Ticker = ticker,
                            Message = $"Price {price.Close} is breaking above down trend line {lineStart.High} - {lineEnd.High}",
                            CreatedAt = DateTime.Now,
                            Strategy = "SwingPointsLiveTradingStrategy",
                            OrderType = OrderType.Long,
                            OrderAction = OrderAction.Open
                        };

                        OnAlertCreated(new AlertEventArgs(alert));
                    }
                }
            }
        }

        public void CheckForBreakBelowUpTrendLine(string ticker, List<Price> ascSortedByDatePrice, IStrategyParameter strategyParameter)
        {
            var parameter = (SwingPointStrategyParameter)strategyParameter;
            var numberOfCandlesticksToLookBack = parameter.NumberOfCandlesticksToLookBack;
            var lowLines = SwingPointAnalyzer.GetTrendlines(ascSortedByDatePrice, numberOfCandlesticksToLookBack, false);
            var trendingUpLines = lowLines.Where(x => x.Item2.Low > x.Item1.Low).ToList();

            var secondLastPrice = ascSortedByDatePrice[ascSortedByDatePrice.Count - 2];
            var price = ascSortedByDatePrice.Last();

            foreach (var lineEnds in trendingUpLines)
            {
                var lineStart = lineEnds.Item1;
                var lineEnd = lineEnds.Item2;
                var lineStartIndex = ascSortedByDatePrice.IndexOf(lineStart);
                var lineEndIndex = ascSortedByDatePrice.IndexOf(lineEnd);
                var lineStartPoint = new Point(lineStartIndex, lineStart.Low);
                var lineEndPoint = new Point(lineEndIndex, lineEnd.Low);
                var line = new Line(lineStartPoint, lineEndPoint);

                var secondLastPointIndex = ascSortedByDatePrice.IndexOf(secondLastPrice);

                var projectedYForSecondLastPriceLine = line.FindYAtX(secondLastPointIndex);
                var projectedYForCurrentPriceLine = line.FindYAtX(ascSortedByDatePrice.Count - 1);

                var secondLastHighPoint = new Point(secondLastPointIndex, secondLastPrice.High);
                var secondLastLowPoint = new Point(secondLastPointIndex, secondLastPrice.Low);
                var currentHighPoint = new Point(ascSortedByDatePrice.Count - 1, price.High);
                var currentLowPoint = new Point(ascSortedByDatePrice.Count - 1, price.Low);

                // this wont work be cause the line is not running to to current price
                // update so that we can extend the line to the current price, also check for price completely cross the line, TSLA 2023-11-20 12:45
                // or check if the price touch and bounce of the line
                var crossSecondLastPrice = SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(secondLastPointIndex, projectedYForSecondLastPriceLine), secondLastLowPoint, secondLastHighPoint);
                var notCrossCurrentPrice = !SwingPointAnalyzer.DoLinesIntersect(lineStartPoint, new Point(ascSortedByDatePrice.Count - 1, projectedYForCurrentPriceLine), currentLowPoint, currentHighPoint);
                var priceBelowLine = price.Close < projectedYForCurrentPriceLine && price.Open < projectedYForCurrentPriceLine;
                if (crossSecondLastPrice && notCrossCurrentPrice && priceBelowLine)
                {
                    var alert = new Alert
                    {
                        Ticker = ticker,
                        Message = $"Price {price.Close} ({price.Date:s}) is breaking below up trend line {lineStart.Low} ({lineEnds.Item1.Date:s}) - {lineEnd.Low} ({lineEnds.Item2.Date:s})",
                        CreatedAt = DateTime.Now,
                        Strategy = "SwingPointsLiveTradingStrategy",
                        OrderType = OrderType.Short,
                        OrderAction = OrderAction.Open,
                        Timeframe = parameter.Timeframe
                    };

                    OnAlertCreated(new AlertEventArgs(alert));
                }
            }
        }

        protected virtual void OnAlertCreated(AlertEventArgs e)
        {
            AlertCreated?.Invoke(this, e);
        }
    }
}
