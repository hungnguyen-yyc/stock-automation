using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace StockSignalScanner.Models
{
    public class StockDataAggregator : StockInfo
    {
        private static readonly int[] CROSSES_IN_LAST_DAYS = new int[] { 14, 5 }; // TODO: update StockData to handle crosses value better
        private readonly IList<IPrice> _priceOrderByDateAsc;
        private readonly int _rsiPeriod;
        private readonly int _macdShortPeriod;
        private readonly int _macdLongPeriod;
        private readonly int _macdSignalPeriod;
        private readonly int _stochasticPeriod;
        private readonly int _smoothK;
        private readonly int _smoothD;

        public StockDataAggregator(string ticker, IList<HistoricalPrice> prices, int rsiPeriod, int macdShortPeriod, int macdLongPeriod, int macdSignalPeriod, int stochasticPeriod, int smoothK, int smoothD)
        {
            Symbol = ticker;
            Candles = new List<StockDataCandle>();
            _rsiPeriod = rsiPeriod;
            _macdShortPeriod = macdShortPeriod;
            _macdLongPeriod = macdLongPeriod;
            _macdSignalPeriod = macdSignalPeriod;
            _stochasticPeriod = stochasticPeriod;
            _smoothD = smoothD;
            _smoothK = smoothK;
            _priceOrderByDateAsc = prices
                .OrderBy(p => p.Date)
                .Select(p => (IPrice)new HistoricalPrice
                {
                    Date = p.Date,
                    AdjClose = p.AdjClose,
                    Close = p.Close,
                    Change = p.Change,
                    ChangeOverTime = p.ChangeOverTime,
                    ChangePercent = p.ChangePercent,
                    High = p.High,
                    Label = p.Label,
                    Low = p.Low,
                    Open = p.Open,
                    UnadjustedVolume = p.UnadjustedVolume,
                    Volume = p.Volume,
                    Vwap = p.Vwap,
                })
                .ToList();
            Aggregate();
        }

        public bool StochasticInOverboughtLast5Days { get; private set; }
        public bool StochasticInOversoldLast5Days { get; private set;  }
        public bool StochasticInOverboughtLast14Days { get; private set; }
        public bool StochasticInOversoldLast14Days { get; private set; }
        public List<StockDataCandle> Candles { get; }

        public CrossDirection RSICrossDirectionLast14Days { get; private set; }
        public CrossDirection MACDCrossDirectionLast14Days { get; private set; }
        public CrossDirection StochCrossDirectionLast14Days { get; private set; }
        public CrossDirection RSICrossDirectionLast5Days { get; private set; }
        public CrossDirection MACDCrossDirectionLast5Days { get; private set; }
        public CrossDirection StochCrossDirectionLast5Days { get; private set; }

        public bool MACDRSICrossesAbove14 => MACDCrossDirectionLast14Days == CrossDirection.CROSS_ABOVE
                                            && RSICrossDirectionLast14Days == CrossDirection.CROSS_ABOVE;

        public bool MACDRSICrossesBelow14 => MACDCrossDirectionLast14Days == CrossDirection.CROSS_BELOW
                                            && RSICrossDirectionLast14Days == CrossDirection.CROSS_BELOW;

        public bool MACDRSICrossesAbove5 => MACDCrossDirectionLast5Days == CrossDirection.CROSS_ABOVE
                                            && RSICrossDirectionLast5Days == CrossDirection.CROSS_ABOVE;

        public bool MACDRSICrossesBelow5 => MACDCrossDirectionLast5Days == CrossDirection.CROSS_BELOW
                                            && RSICrossDirectionLast5Days == CrossDirection.CROSS_BELOW;

        public bool AllCrossesAbove14 => MACDCrossDirectionLast14Days == CrossDirection.CROSS_ABOVE
                                            && RSICrossDirectionLast14Days == CrossDirection.CROSS_ABOVE
                                            && StochCrossDirectionLast14Days == CrossDirection.CROSS_ABOVE;

        public bool AllCrossesBelow14 => MACDCrossDirectionLast14Days == CrossDirection.CROSS_BELOW
                                            && RSICrossDirectionLast14Days == CrossDirection.CROSS_BELOW
                                            && StochCrossDirectionLast14Days == CrossDirection.CROSS_BELOW;

        public bool AllCrossesAbove5 => MACDCrossDirectionLast5Days == CrossDirection.CROSS_ABOVE
                                            && RSICrossDirectionLast5Days == CrossDirection.CROSS_ABOVE
                                            && StochCrossDirectionLast5Days == CrossDirection.CROSS_ABOVE;

        public bool AllCrossesBelow5 => MACDCrossDirectionLast5Days == CrossDirection.CROSS_BELOW
                                            && RSICrossDirectionLast5Days == CrossDirection.CROSS_BELOW
                                            && StochCrossDirectionLast5Days == CrossDirection.CROSS_BELOW;

        public override string ToString()
        {
            return $"{Symbol},{ExchangeShortName},{RSICrossDirectionLast14Days},{StochCrossDirectionLast14Days},{MACDCrossDirectionLast14Days}";
        }

        public string GetRecommendTickerAction()
        {
            return $"{Symbol}_RSI_{RSICrossDirectionLast14Days}_MACD_{MACDCrossDirectionLast14Days}_STOCHASTICS_{StochCrossDirectionLast14Days}";
        }

        /**
         * this strategy is check for MACD cross
         * if cross above or enter bullish trend
         *  - check for stochastics for in last n days from the cross to be oversold <= 20
         *  - check for in n days, there is no opposite cross in MACD to prevent fake cross
         *    e.g: if cross above and in 5 days if there's a cross under, that means it can be a fake cross
         * if cross below or enter bearish trend, we check for stoch for in last n days from the cross to be overbougt >= 80
         *  - check for stochastics for in last n days from the cross to be overbougt >= 80
         *  - check for in n days, there is no opposite cross in MACD to prevent fake cross
         *    e.g: if cross above and in 5 days if there's a cross under, that means it can be a fake cross
         */
        public bool HasOverboughtOrOversoldFollowedByMACDCrossLastNDays(int n = 5)
        {
            var roomToSkip = -5 - n; // n is the number we want to check for cross in, but we also want to check for a number before that(5);
            (List<decimal> macdValues, List<decimal> signalValues, List<DateTime> macdTimes) = MACDIndicator.GetMACD(_priceOrderByDateAsc, _macdShortPeriod, _macdLongPeriod, _macdSignalPeriod);
            (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) = StochasticIndicator.GetStochastic(_priceOrderByDateAsc, _stochasticPeriod, _smoothK, _smoothD);
            var macdLine = macdTimes
                .Zip(macdValues, (t, v) => (t, v))
                .Skip(macdValues.Count + roomToSkip)
                .ToList();
            var signalLine = macdTimes
                .Zip(signalValues, (t, v) => (t, v))
                .Skip(signalValues.Count + roomToSkip)
                .ToList();
            var kLine = stochasticTimes
                .Zip(kValues, (t, v) => (t, v))
                .ToList();
            var dLine = stochasticTimes
                .Zip(dValues, (t, v) => (t, v))
                .ToList();
            var crosses = CrossDirectionDetector.GetCrossDirectionWithTime(macdLine, signalLine);
            var latestCrossTime = crosses.LastOrDefault(c => c.Value != CrossDirection.NO_CROSS).Key;
            var latestCrossDirection = crosses.LastOrDefault(c => c.Value != CrossDirection.NO_CROSS).Value;
            // should check for stoch as well?
            if (latestCrossTime != default) 
            {
                var direction = crosses.Last().Value;
                var index = stochasticTimes.LastIndexOf(latestCrossTime);
                if (index + roomToSkip < 0)
                {
                    return false;
                }

                foreach (var cross in crosses)
                {
                    // check for no cross because we only get N latest day, so when we do cross direction check, it should be no cross or same with last cross direction
                    if(cross.Key.CompareTo(latestCrossTime) < 0 && cross.Value != CrossDirection.NO_CROSS && cross.Value != latestCrossDirection) 
                    {
                        return false;
                    }
                }
                if (direction.Equals(CrossDirection.CROSS_BELOW)) 
                {
                    return kLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(k => k >= 70) && dLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(d => d >= 70); // suppose to be 80 but change to 70 because overbought/reversal likely to be over 70
                }
                if (direction.Equals(CrossDirection.CROSS_ABOVE))
                {
                    return kLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(k => k <= 20) && dLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(d => d <= 20);
                }
                return false;
            }
            return false;
        }

        private void Aggregate()
        {
            (List<decimal> rsiValues, List<DateTime> rsiTimes) = RSIIndicator.GetRSI(_priceOrderByDateAsc, _rsiPeriod);
            (List<decimal> macdValues, List<decimal> macdSignalValues, List<DateTime> macdTimes) = MACDIndicator.GetMACD(_priceOrderByDateAsc, _macdShortPeriod, _macdLongPeriod, _macdSignalPeriod);
            (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) = StochasticIndicator.GetStochastic(_priceOrderByDateAsc, _stochasticPeriod);

            foreach (var days in CROSSES_IN_LAST_DAYS)
            {
                List<(DateTime, decimal)> macdLine = macdTimes.Zip(macdValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> signalLine = macdTimes.Zip(macdSignalValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> kLine = stochasticTimes.Zip(kValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> dLine = stochasticTimes.Zip(dValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> rsiLine = rsiTimes.Zip(rsiValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
                List<(DateTime, decimal)> rsi50Line = rsiTimes.Select(r => (r, 50m)).Take(days).ToList();

                // base on this https://www.youtube.com/watch?v=510G39RXuPE&t=421s
                if (days == 14)
                {
                    MACDCrossDirectionLast14Days = CrossDirectionDetector.GetCrossDirection(macdLine, signalLine);
                    StochCrossDirectionLast14Days = CrossDirectionDetector.GetCrossDirection(kLine, dLine);
                    RSICrossDirectionLast14Days = CrossDirectionDetector.GetCrossDirection(rsiLine, rsi50Line);
                    StochasticInOverboughtLast14Days = kLine.Select(k => k.Item2).Any(k => k >= 80) && dLine.Select(d => d.Item2).Any(d => d >= 80);
                    StochasticInOversoldLast14Days = kLine.Select(k => k.Item2).Any(k => k <= 20m) && dLine.Select(d => d.Item2).Any(d => d <= 20);
                }
                if (days == 5)
                {
                    MACDCrossDirectionLast5Days = CrossDirectionDetector.GetCrossDirection(macdLine, signalLine);
                    StochCrossDirectionLast5Days = CrossDirectionDetector.GetCrossDirection(kLine, dLine);
                    RSICrossDirectionLast5Days = CrossDirectionDetector.GetCrossDirection(rsiLine, rsi50Line);
                    StochasticInOverboughtLast5Days = kLine.Select(k => k.Item2).Any(k => k >= 80) && dLine.Select(d => d.Item2).Any(d => d >= 80);
                    StochasticInOversoldLast5Days = kLine.Select(k => k.Item2).Any(k => k <= 20) && dLine.Select(d => d.Item2).Any(d => d <= 20);
                }
            }

            // Loop through the RSI values
            for (int i = 0; i < rsiValues.Count; i++)
            {
                // Get the RSI, MACD, and stochastic values for the current time period
                IPrice price = _priceOrderByDateAsc[i];
                decimal rsiValue = rsiValues[i];
                decimal macdValue = macdValues[i];
                decimal macdSignalValue = macdSignalValues[i];
                decimal stochasticKValue = kValues[i];
                decimal stochasticDValue = dValues[i];

                Candles.Add(new StockDataCandle
                {
                    Date = price.Date.DateTime,
                    MACD = macdValue,
                    Signal = macdSignalValue,
                    RSI = rsiValue,
                    StochasticK = stochasticKValue,
                    StochasticD = stochasticDValue,
                    Symbol = Symbol,
                    PriceClose = price.Close,
                    Volume = price.Volume,
                });
            }
        }
    }

    public class StockDataCandle : StockInfo
    {
        public DateTime Date { get; set; }
        public decimal RSI { get; set; }
        public decimal StochasticK { get; set; }
        public decimal StochasticD { get; set; }
        public decimal MACD { get; set; }
        public decimal Signal { get; set; }
        public decimal PriceClose { get; set; }
        public decimal Volume { get; set; }
        
        public MACDTrend MACDStatus
        {
            get
            {
                if (MACD > Signal)
                {
                    return MACDTrend.BULLISH;
                }
                else if (MACD < Signal)
                {
                    return MACDTrend.BEARISH;
                }
                return MACDTrend.MEET;
            }
        }
        public RSIStatus RSIStatus
        {
            get
            {
                if (RSI <= 30)
                {
                    return RSIStatus.OVERSOLD;
                }
                else if (RSI >= 70)
                {
                    return RSIStatus.OVERBOUGHT;
                }
                return RSIStatus.MIXED;
            }
        }
        public RSIStatus StochStatus
        {
            get
            {
                if (StochasticD >= 80 && StochasticK >= 80)
                {
                    return RSIStatus.OVERBOUGHT;
                }

                if (StochasticD <= 20 && StochasticK <= 20)
                {
                    return RSIStatus.OVERSOLD;
                }

                return RSIStatus.MIXED;
            }
        }
    }
}
