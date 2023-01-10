using Newtonsoft.Json.Linq;
using StockSignalScanner.Indicators;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;

namespace StockSignalScanner.Models
{
    public class StockDataAggregator : StockInfo
    {
        private readonly IList<IPrice> _priceOrderByDateAsc;
        private readonly int _rsiPeriod;
        private readonly int _macdShortPeriod;
        private readonly int _macdLongPeriod;
        private readonly int _macdSignalPeriod;
        private readonly int _stochasticPeriod;
        private readonly int _smoothK;
        private readonly int _smoothD;
        List<decimal> rsiValues;
        List<DateTime> rsiTimes;
        List<decimal> macdValues;
        List<decimal> macdSignalValues;
        List<DateTime> macdTimes;
        List<decimal> kValues;
        List<decimal> dValues;
        List<DateTime> stochasticTimes;

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

        public List<StockDataCandle> Candles { get; }

        public bool CheckAllCrossesWithDirectionInLastNDays(int days, CrossDirection direction)
        {
            return GetMACDCrossDirectionInLastNDays(days) == direction
                                            && GetRSICross50DirectionInLastNDays(days) == direction
                                            && GetStochasticCrossDirectionInLastNDays(days) == direction;
        }
        public string TrendlineHigh(int lastNDays)
        {
            var timeSpan = new TimeSpan(lastNDays, 0, 0, 0);
            var start = DateTimeOffset.Now.Subtract(timeSpan);
            var end = DateTimeOffset.Now;
            // var bars = LinearTrendlineDetector.FindTrendline(_priceOrderByDateAsc.Select(s => (s.Date, s.High)).ToList(), out start, out end);
            return $"{start.ToString("yyyy-MM-dd")}-{end.ToString("yyyy-MM-dd")}";
        }

        public CurrentPriceFibonacciRetracementLevel GetCurrentFibonacciRetracementLevelLastNDays(int startIndex)
        {
            return Fibonacci.GetCurrentPriceState(_priceOrderByDateAsc, startIndex, _priceOrderByDateAsc.Count() - 1);
        }

        public ZoneState CheckInSupportZoneLastNDays(int period)
        {
            return SupportZone.IsInSupportZone(_priceOrderByDateAsc, period);
        }

        public ZoneState CheckInResistanceZoneLastNDays(int period)
        {
            return ResistanceZone.IsInResistanceZone(_priceOrderByDateAsc, period);
        }

        public string GetTickerStatusLastNDays(int days)
        {
            var patterns = CandlestickPatternsLastNDays(days);
            var patternString = string.Join(" - ", patterns.Select(s => s.ToString()).ToArray());
            return $"{Symbol}_MACD_{GetMACDCrossDirectionInLastNDays(days)}_RSI_{GetRSICross50DirectionInLastNDays(days)}_STOCHASTICS_{GetStochasticCrossDirectionInLastNDays(days)}_PATTERNS_{patternString}";
        }

        private IEnumerable<CandlestickPatternType> CandlestickPatternsLastNDays(int days)
        {
            var prices = _priceOrderByDateAsc
                .Skip(_priceOrderByDateAsc.Count() - days)
                .Take(days)
                .ToList();
            return CandlestickPatternDetector.Detect(prices);
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
        public bool HasOverboughtOrOversoldFollowedByMACDCrossLastNDays(int n = 5, int margin = 5)
        {
            /**
             * n is the number we want to check for cross in, 
             * but we also want to check for a number before that (margin)
             * just to make sure lines explicit cross eachother
             */
            var roomToSkip = - margin - n; 
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
                    var overbought = kLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(k => k >= 70) && dLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(d => d >= 70); // suppose to be 80 but change to 70 because overbought/reversal likely to be over 70
                    return overbought && GetStochasticCrossDirectionInLastNDays(n) == CrossDirection.CROSS_BELOW;
                }
                if (direction.Equals(CrossDirection.CROSS_ABOVE))
                {
                    var oversold = kLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(k => k <= 20) && dLine.Skip(index - 1 - n).Take(n).Select(d => d.Item2).All(d => d <= 20);
                    return oversold && GetStochasticCrossDirectionInLastNDays(n) == CrossDirection.CROSS_ABOVE;
                }
                return false;
            }
            return false;
        }

        public CrossDirection GetMACDCrossDirectionInLastNDays(int days)
        {
            List<(DateTime, decimal)> macdLine = macdTimes.Zip(macdValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> signalLine = macdTimes.Zip(macdSignalValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            return CrossDirectionDetector.GetCrossDirection(macdLine, signalLine);
        }

        public CrossDirection GetStochasticCrossDirectionInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = stochasticTimes.Zip(kValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = stochasticTimes.Zip(dValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            return CrossDirectionDetector.GetCrossDirection(kLine, dLine);
        }

        public bool IsOverboughtByStochasticInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = stochasticTimes.Zip(kValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = stochasticTimes.Zip(dValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            return kLine.Select(k => k.Item2).Any(k => k >= 80) && dLine.Select(d => d.Item2).Any(d => d >= 80);
        }

        public bool IsOversoldByStochasticInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = stochasticTimes.Zip(kValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = stochasticTimes.Zip(dValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            return kLine.Select(k => k.Item2).Any(k => k <= 20m) && dLine.Select(d => d.Item2).Any(d => d <= 20);
        }

        public CrossDirection GetRSICross50DirectionInLastNDays(int days)
        {
            List<(DateTime, decimal)> rsiLine = rsiTimes.Zip(rsiValues, (t, v) => (t, v)).Skip(macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> rsi50Line = rsiTimes.Select(r => (r, 50m)).Take(days).ToList();
            return CrossDirectionDetector.GetCrossDirection(rsiLine, rsi50Line);
        }

        private void Aggregate()
        {
            (rsiValues, rsiTimes) = RSIIndicator.GetRSI(_priceOrderByDateAsc, _rsiPeriod);
            (macdValues, macdSignalValues, macdTimes) = MACDIndicator.GetMACD(_priceOrderByDateAsc, _macdShortPeriod, _macdLongPeriod, _macdSignalPeriod);
            (kValues, dValues, stochasticTimes) = StochasticIndicator.GetStochastic(_priceOrderByDateAsc, _stochasticPeriod, _smoothK, _smoothD);

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
