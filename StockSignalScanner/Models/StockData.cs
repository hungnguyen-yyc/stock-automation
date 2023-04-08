using Newtonsoft.Json.Linq;
using Skender.Stock.Indicators;
using StockSignalScanner.Indicators;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace StockSignalScanner.Models
{
    public class StockDataAggregator : SymbolInfo
    {
        private readonly IList<IPrice> _priceOrderByDateAsc;
        private readonly IEnumerable<Quote> _quotes;
        private readonly int _rsiPeriod;
        private readonly int _rsiMA;
        private readonly int _macdShortPeriod;
        private readonly int _macdLongPeriod;
        private readonly int _macdSignalPeriod;
        private readonly int _stochasticPeriod;
        private readonly int _smoothK;
        private readonly int _smoothD;
        private readonly int _adxPeriod;
        List<decimal> _rsiValues;
        List<decimal> _rsiMAValues;
        List<DateTime> _rsiTimes;
        List<decimal> _macdValues;
        List<decimal> _macdSignalValues;
        List<DateTime> _macdTimes;
        List<decimal> _kValues;
        List<decimal> _dValues;
        List<DateTime> _stochasticTimes;
        Dictionary<int, CrossDirection> _macdCrossLastNdDaysMap;
        Dictionary<int, CrossDirection> _stochasticCrossLastNdDaysMap;
        Dictionary<int, CrossDirection> _rsiCrossLastNdDaysMap;
        DMI _dmi;

        public StockDataAggregator(string ticker, string exchange, IList<HistoricalPrice> prices, int rsiPeriod, int rsiMA, int macdShortPeriod, int macdLongPeriod, int macdSignalPeriod, int stochasticPeriod, int smoothK, int smoothD, int adxPeriod)
        {
            Symbol = ticker;
            Exchange = exchange;
            Candles = new List<StockDataCandle>();
            _macdCrossLastNdDaysMap = new Dictionary<int, CrossDirection>();
            _stochasticCrossLastNdDaysMap = new Dictionary<int, CrossDirection>();
            _rsiCrossLastNdDaysMap = new Dictionary<int, CrossDirection>();
            _rsiPeriod = rsiPeriod;
            _rsiMA = rsiMA;
            _macdShortPeriod = macdShortPeriod;
            _macdLongPeriod = macdLongPeriod;
            _macdSignalPeriod = macdSignalPeriod;
            _stochasticPeriod = stochasticPeriod;
            _smoothD = smoothD;
            _adxPeriod = adxPeriod;
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

            _quotes = _priceOrderByDateAsc.Select(p => p.ToQuote());
        }

        private List<StockDataCandle> Candles { get; }
        public string Exchange { get; }
        public int NumberOfTradingDays => PriceOrderByDateAsc.Count;

        public DMI Dmi { get => _dmi; }

        public IEnumerable<Quote> Quotes => _quotes;

        public IReadOnlyList<IPrice> PriceOrderByDateAsc => _priceOrderByDateAsc as IReadOnlyList<IPrice>;

        public bool CheckAllCrossesWithDirectionInLastNDays(int days, CrossDirection direction)
        {
            return GetMACDCrossDirectionInLastNDays(days) == direction
                                            && GetRSICrossRSIMADirectionInLastNDays(days) == direction
                                            && GetStochasticCrossDirectionInLastNDays(days) == direction;
        }

        public CrossDirection CheckEMACrossInLastNDays(int days, int periodFast, int periodSlow, int differentBetweenFastAndSlowInPercentage = 0)
        {
            List<decimal> periodALine = MovingAverage.CalculateEMA(PriceOrderByDateAsc.Select(i => i.Close).ToList(), periodFast);
            List<decimal> periodBLine = MovingAverage.CalculateEMA(PriceOrderByDateAsc.Select(i => i.Close).ToList(), periodSlow);
            List<decimal> periodALineLastNDays = periodALine.Skip(periodALine.Count - days).ToList();
            List<decimal> periodBLineLastNDays = periodBLine.Skip(periodBLine.Count - days).ToList();
            var direction = CrossDirectionDetector.GetCrossDirection(periodALineLastNDays, periodBLineLastNDays);
            if (differentBetweenFastAndSlowInPercentage == 0)
            {
                return direction;
            }
            else
            {
                var a = periodALineLastNDays.Last();
                var b = periodBLineLastNDays.Last();
                var differentPercentage = 100 * Math.Abs(a - b) / ((a + b) / 2);
                if (differentPercentage >= differentBetweenFastAndSlowInPercentage)
                {
                    return direction;
                }
                else
                {
                    return CrossDirection.NO_CROSS;
                }
            }
        }

        public bool CheckPriceTouchEMAInLastNDays(int days, int emaPeriod)
        {
            try
            {
                List<decimal> periodALine = MovingAverage.CalculateEMAV5(PriceOrderByDateAsc.Select(i => i.Close).ToList(), emaPeriod);
                List<decimal> periodALineLastNDays = periodALine.Skip(periodALine.Count - days).ToList();
                var prices = PriceOrderByDateAsc.Skip(PriceOrderByDateAsc.Count - days).ToList();
                for (int i = 0; i < prices.Count; i++)
                {
                    IPrice price = prices[i];
                    var ema = periodALineLastNDays[i];
                    if (ema >= price.Low && ema <= price.High)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public Trend GetTrendInLastNDays(int days)
        {
            var detector = new NewHighLowDetector();
            return detector.GetTrendReversal(PriceOrderByDateAsc.Skip(PriceOrderByDateAsc.Count - days).ToList());
        }

        public CrossDirection CheckSTCCrossInLastNDays(decimal level, int days, int maShort, int maLong, int cycle, decimal factor = 0.5m)
        {
            var stc = SchaffTrendCycle.CalculateSTC(PriceOrderByDateAsc.ToList(), maShort, maLong, cycle, factor).Skip(PriceOrderByDateAsc.Count() - days).ToList();
            var levels = stc.Select(s => level).ToList();
            return CrossDirectionDetector.GetCrossDirection(stc, levels);
        }

        public CrossDirection GetMFICrossLevelInLastNDays(int period, int level, int days)
        {
            var calculated = Quotes.GetMfi(period);
            var indicatorData = calculated.Select(m => m.Mfi ?? 0).Skip(calculated.Count() - days).ToList();
            var levels = indicatorData.Select(m => (double)level).ToList();
            return CrossDirectionDetector.GetCrossDirection(indicatorData, levels);
        }

        public CrossDirection GetAroonOscillatorCrossLevelInLastNDays(int period, int level, int days)
        {
            var calculated = Quotes.GetAroon(14);
            var indicatorData = calculated.Select(m => m.Oscillator ?? 0).Skip(calculated.Count() - days).ToList();
            var levels = indicatorData.Select(m => (double)level).ToList();
            return CrossDirectionDetector.GetCrossDirection(indicatorData, levels);
        }

        public CrossDirection GeStochasticMomentumIndexCrossLevelInLastNDays(int period, int level, int days)
        {
            var calculated = Quotes.GetSmi(10, 3, 3);
            var indicatorData = calculated.Select(m => m.Smi ?? 0).Skip(calculated.Count() - days).ToList();
            var levels = indicatorData.Select(m => (double)level).ToList();
            return CrossDirectionDetector.GetCrossDirection(indicatorData, levels);
        }

        public List<double> GetVolumeWeightedMovingAverageInLastNDays(int period)
        {
            var calculated = Quotes.GetVwma(period);
            var indicatorData = calculated.Select(m => m.Vwma ?? 0).ToList();
            return indicatorData;
        }

        public List<(DateTime, CrossDirection)> CheckAllEMACrossInLastNDays(int days, int periodA, int periodB)
        {
            var pricesInPeriod = PriceOrderByDateAsc.Skip(PriceOrderByDateAsc.Count() - days).ToList();
            List<decimal> periodALine = MovingAverage.CalculateEMA(PriceOrderByDateAsc.Select(i => i.Close).ToList(), periodA).Skip(PriceOrderByDateAsc.Count() - days).ToList();
            List<decimal> periodBLine = MovingAverage.CalculateEMA(PriceOrderByDateAsc.Select(i => i.Close).ToList(), periodB).Skip(PriceOrderByDateAsc.Count() - days).ToList();
            List<(DateTime, decimal)> periodALineLastNDays = pricesInPeriod.Select(i => i.Date.Date).Zip(periodALine, (t,v) => (t,v)).ToList();
            List<(DateTime, decimal)> periodBLineLastNDays = pricesInPeriod.Select(i => i.Date.Date).Zip(periodBLine, (t, v) => (t, v)).ToList();
            var direction = CrossDirectionDetector.GetAllCrossDirections(periodALineLastNDays, periodBLineLastNDays);
            return direction;
        }

        public List<decimal> GetPricesInPeriod(DateTime startDate, int period)
        {
            var list = new List<decimal>();
            var price = PriceOrderByDateAsc.FirstOrDefault(i => i.Date.Date == startDate);
            var i = 1;
            while (price == null)
            {
                price = PriceOrderByDateAsc.FirstOrDefault(p => p.Date.Date == startDate.AddDays(i * (-1)));
                i++;
                if (i > 14)
                {
                    return list;
                }
            }
            return PriceOrderByDateAsc.Where(p => p.Date.Date >= startDate && p.Date.Date < startDate.AddDays(period)).Select(p => p.Close).ToList();
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
            return Fibonacci.GetCurrentPriceState(PriceOrderByDateAsc.ToList(), startIndex, PriceOrderByDateAsc.Count() - 1);
        }

        public ZoneState CheckInSupportZoneLastNDays(int period)
        {
            return SupportZone.IsInSupportZone(PriceOrderByDateAsc.ToList(), period);
        }

        public ZoneState CheckInResistanceZoneLastNDays(int period)
        {
            return ResistanceZone.IsInResistanceZone(PriceOrderByDateAsc.ToList(), period);
        }

        public string GetTickerStatusLastNDays(int days)
        {
            if (!_macdCrossLastNdDaysMap.ContainsKey(days))
            {
                GetMACDCrossDirectionInLastNDays(days);
            }
            if (!_stochasticCrossLastNdDaysMap.ContainsKey(days))
            {
                GetStochasticCrossDirectionInLastNDays(days);
            }
            if (!_rsiCrossLastNdDaysMap.ContainsKey(days))
            {
                GetRSICrossRSIMADirectionInLastNDays(days);
            }
            var patterns = CandlestickPatternsLastNDays(days);
            var patternString = string.Join(" - ", patterns.Select(s => s.ToString()).ToArray());
            var fibo = GetCurrentFibonacciRetracementLevelLastNDays(30);
            return $"{Symbol},{Exchange}" +
                $",{_macdCrossLastNdDaysMap[days]}" +
                $",{_rsiCrossLastNdDaysMap[days]}" +
                $",{_stochasticCrossLastNdDaysMap[days]}" +
                $",{patternString}" +
                $",{string.Join("-", PriceOrderByDateAsc.TakeLast(days).Select(p => p.Close.ToString()))}" +
                $",{fibo?.RetracementLevel}-l: {fibo?.Retracement.Low}-h: {fibo?.Retracement.High}";
        }

        private IEnumerable<CandlestickPatternType> CandlestickPatternsLastNDays(int days)
        {
            var prices = PriceOrderByDateAsc
                .Skip(PriceOrderByDateAsc.Count() - days)
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
            (List<decimal> macdValues, List<decimal> signalValues, List<DateTime> macdTimes) = MACDIndicator.GetMACD(PriceOrderByDateAsc.ToList(), _macdShortPeriod, _macdLongPeriod, _macdSignalPeriod);
            (List<decimal> kValues, List<decimal> dValues, List<DateTime> stochasticTimes) = StochasticIndicator.GetStochastic(PriceOrderByDateAsc.ToList(), _stochasticPeriod, _smoothK, _smoothD);
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
            List<(DateTime, decimal)> macdLine = _macdTimes.Zip(_macdValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> signalLine = _macdTimes.Zip(_macdSignalValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            var direction = CrossDirectionDetector.GetCrossDirection(macdLine, signalLine);
            if (_macdCrossLastNdDaysMap.ContainsKey(days))
            {
                _macdCrossLastNdDaysMap.Remove(days);
            }
            _macdCrossLastNdDaysMap.Add(days, direction);
            return direction;
        }

        public CrossDirection GetMACDCrossZeroDirectionInLastNDays(int days)
        {
            List<(DateTime, decimal)> macdLine = _macdTimes.Zip(_macdValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> zeroLine = _macdTimes.Select(i => (i, 0m)).ToList();
            var direction = CrossDirectionDetector.GetCrossDirection(macdLine, zeroLine);
            if (_macdCrossLastNdDaysMap.ContainsKey(days))
            {
                _macdCrossLastNdDaysMap.Remove(days);
            }
            _macdCrossLastNdDaysMap.Add(days, direction);
            return direction;
        }

        public IEnumerable<decimal> GetMACDHistogramInLastNDays(int days)
        {
            List<decimal> macdLine = _macdValues.Skip(_macdTimes.Count - days).ToList();
            List<decimal> signalLine = _macdSignalValues.Skip(_macdTimes.Count - days).ToList();
            return macdLine.Zip(signalLine, (t,v) => (t,v)).Select(i => i.t - i.v);
        }

        public IEnumerable<decimal> GetADXInLastNDays(int days)
        {
            List<decimal> adx = Dmi.Adx.Skip(Dmi.Adx.Count - days).ToList();
            return adx;
        }

        public decimal GetADXAtDate(DateTime date)
        {
            var dateIndex = PriceOrderByDateAsc.Select(i => i.Date).ToList().IndexOf(date);
            return Dmi.Adx[dateIndex];
        }

        public (decimal, decimal) GetLowestMACDHistogramInLastNDays(int days)
        {
            var macdLine = _macdValues.Skip(_macdValues.Count - days).Min();
            var signalLine = _macdSignalValues.Skip(_macdSignalValues.Count - days).Min();
            return (macdLine, signalLine);
        }

        public (decimal, decimal) GetHighestMACDHistogramInLastNDays(int days)
        {
            var macdLine = _macdValues.Skip(_macdValues.Count - days).Max();
            var signalLine = _macdSignalValues.Skip(_macdSignalValues.Count - days).Max();
            return (macdLine, signalLine);
        }

        public CrossDirection GetStochasticCrossDirectionInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = _stochasticTimes.Zip(_kValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = _stochasticTimes.Zip(_dValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            var direction = CrossDirectionDetector.GetCrossDirection(kLine, dLine);
            if (_stochasticCrossLastNdDaysMap.ContainsKey(days))
            {
                _stochasticCrossLastNdDaysMap.Remove(days);
            }
            _stochasticCrossLastNdDaysMap.Add(days, direction);
            return direction;
        }

        public bool IsOverboughtByStochasticInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = _stochasticTimes.Zip(_kValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = _stochasticTimes.Zip(_dValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            return kLine.Select(k => k.Item2).Any(k => k >= 80) && dLine.Select(d => d.Item2).Any(d => d >= 80);
        }

        public bool IsOversoldByStochasticInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = _stochasticTimes.Zip(_kValues, (t, v) => (t, v)).Skip(_stochasticTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = _stochasticTimes.Zip(_dValues, (t, v) => (t, v)).Skip(_stochasticTimes.Count - days).ToList();
            return kLine.Select(k => k.Item2).Any(k => k <= 20m) && dLine.Select(d => d.Item2).Any(d => d <= 20);
        }

        public bool IsOverboughtByRSIInLastNDays(int days)
        {
            List<(DateTime, decimal)> rsiLine = _rsiTimes.Zip(_rsiValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            List<(DateTime, decimal)> rsiMALine = _rsiTimes.Zip(_rsiMAValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            return rsiLine.Select(k => k.Item2).Any(k => k >= 70) && rsiMALine.Select(d => d.Item2).Any(d => d >= 70);
        }

        public bool IsBullishByRSIInLastNDays(int days)
        {
            List<(DateTime, decimal)> rsiLine = _rsiTimes.Zip(_rsiValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            List<(DateTime, decimal)> rsiMALine = _rsiTimes.Zip(_rsiMAValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            return rsiLine.Select(k => k.Item2).All(k => k > 50 && k < 70) && rsiMALine.Select(d => d.Item2).All(k => k > 50 && k < 70);
        }

        public bool IsBearishByRSIInLastNDays(int days)
        {
            List<(DateTime, decimal)> rsiLine = _rsiTimes.Zip(_rsiValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            List<(DateTime, decimal)> rsiMALine = _rsiTimes.Zip(_rsiMAValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            return rsiLine.Select(k => k.Item2).All(k => k > 30 && k < 50) && rsiMALine.Select(d => d.Item2).All(k => k > 30 && k < 50);
        }

        public bool IsOversoldByRSIInLastNDays(int days)
        {
            List<(DateTime, decimal)> rsiLine = _rsiTimes.Zip(_rsiValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            List<(DateTime, decimal)> rsiMALine = _rsiTimes.Zip(_rsiMAValues, (t, v) => (t, v)).Skip(_rsiValues.Count - days).ToList();
            return rsiLine.Select(k => k.Item2).Any(k => k <= 30) && rsiMALine.Select(d => d.Item2).Any(d => d <= 30);
        }

        public CrossDirection GetRSICrossRSIMADirectionInLastNDays(int days)
        {
            List<(DateTime, decimal)> rsiLine = _rsiTimes.Zip(_rsiValues, (t, v) => (t, v)).Skip(_rsiTimes.Count - days).ToList();
            List<(DateTime, decimal)> rsiMALine = _rsiTimes.Zip(_rsiMAValues, (t, v) => (t, v)).Skip(_rsiTimes.Count - days).ToList();
            var direction = CrossDirectionDetector.GetCrossDirection(rsiLine, rsiMALine);
            if (_rsiCrossLastNdDaysMap.ContainsKey(days))
            {
                _rsiCrossLastNdDaysMap.Remove(days);
            }
            _rsiCrossLastNdDaysMap.Add(days, direction);
            return direction;
        }

        public DateTime GetDateOfAllCrossesInLastNDays(int days)
        {
            List<(DateTime, decimal)> kLine = _stochasticTimes.Zip(_kValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> dLine = _stochasticTimes.Zip(_dValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            var stochCrossDate = CrossDirectionDetector.GetDateOfCross(kLine, dLine);

            List<(DateTime, decimal)> macdLine = _macdTimes.Zip(_macdValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            List<(DateTime, decimal)> signalLine = _macdTimes.Zip(_macdSignalValues, (t, v) => (t, v)).Skip(_macdTimes.Count - days).ToList();
            var macdCrossDate = CrossDirectionDetector.GetDateOfCross(macdLine, signalLine);

            List<decimal> rsiMAs = MovingAverage.CalculateEMA(_rsiValues, _rsiMA);
            List<(DateTime, decimal)> rsiLine = _rsiTimes.Zip(_rsiValues, (t, v) => (t, v)).Skip(_rsiTimes.Count - days).ToList();
            List<(DateTime, decimal)> rsiMALine = _rsiTimes.Zip(rsiMAs, (t, v) => (t, v)).Skip(_rsiTimes.Count - days).ToList();
            var rsiCrossDate = CrossDirectionDetector.GetDateOfCross(rsiLine, rsiMALine);

            return new [] { stochCrossDate, macdCrossDate, rsiCrossDate }.Max();
        }
    }

    public class StockDataCandle : SymbolInfo
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
