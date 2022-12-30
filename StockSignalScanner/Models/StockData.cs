namespace StockSignalScanner.Models
{
    public class StockData
    {
        public DateTime Date { get; set; }
        public string Ticker { get; set; }
        public string Exchange { get; set; }
        public decimal RSI { get; set; }
        public decimal StochasticK { get; set; }
        public decimal StochasticD { get; set; }
        public decimal MACD { get; set; }
        public decimal Signal { get; set; }
        public decimal PriceClose { get; set; }
        public decimal Volume { get; set; }
        public CrossDirection RSICrossDirectionLast14Days { get; set; }
        public CrossDirection MACDCrossDirectionLast14Days { get; set; }
        public CrossDirection StochCrossDirectionLast14Days { get; set; }
        public CrossDirection RSICrossDirectionLast5Days { get; set; }
        public CrossDirection MACDCrossDirectionLast5Days { get; set; }
        public CrossDirection StochCrossDirectionLast5Days { get; set; }
        public MACDTrend MACDStatus => MACD > Signal ? MACDTrend.BULLISH : MACDTrend.BEARISH;
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

        public bool AllCrossesAbove14WithStochastic => AllCrossesAbove14 && StochasticK <= 20 && StochasticD <= 20;

        public bool AllCrossesBelow14WithStochastic => AllCrossesBelow14 && StochasticK >= 80 && StochasticD >= 80;

        public bool AllCrossesAbove5WithStochastic => AllCrossesAbove5 && StochasticK <= 20 && StochasticD <= 20;

        public bool AllCrossesBelow5WithStochastic => AllCrossesBelow5 && StochasticK >= 80 && StochasticD >= 80;

        public override string ToString()
        {
            return $"{Date.ToString("yyyy-MM-dd-HH-mm-ss")},{Ticker},{Exchange},{PriceClose},{Volume},{RSI},{StochasticK},{StochasticD},{MACD},{Signal},{RSIStatus},{StochStatus},{MACDStatus},{RSICrossDirectionLast14Days},{StochCrossDirectionLast14Days},{MACDCrossDirectionLast14Days}";
        }

        public string GetRecommendTickerAction()
        {
            if (AllCrossesAbove5 || AllCrossesBelow5)
            {
                return $"{Ticker}_RSI_{RSICrossDirectionLast5Days}_MACD_{MACDCrossDirectionLast5Days}_{StochCrossDirectionLast5Days}_{StochStatus}_{Math.Round(StochasticK, 2)}_{Math.Round(StochasticD, 2)}";
            }
            if (AllCrossesAbove14 || AllCrossesBelow14)
            {
                return $"{Ticker}_RSI_{RSICrossDirectionLast14Days}_MACD_{MACDCrossDirectionLast14Days}_{StochCrossDirectionLast14Days}_{StochStatus}_{Math.Round(StochasticK, 2)}_{Math.Round(StochasticD, 2)}";
            }
            return $"{Ticker}_RSI_{RSIStatus}_MACD_{MACDStatus}_{StochCrossDirectionLast14Days}_{StochStatus}_{Math.Round(StochasticK, 2)}_{Math.Round(StochasticD, 2)}";

        }
    }
}
