using Newtonsoft.Json;
using Skender.Stock.Indicators;

namespace Stock.Shared.Models
{
    public class Price : IPrice
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        
        public string DateAsString => Date.ToString("yyyy-MM-dd HH:mm:ss");

        [JsonProperty("open")]
        public decimal Open { get; set; }
        
        [JsonProperty("high")]
        public decimal High { get; set; }
        
        [JsonProperty("low")]
        public decimal Low { get; set; }
        
        [JsonProperty("close")]
        public decimal Close { get; set; }
        
        [JsonProperty("volume")]
        public decimal Volume { get; set; }
        
        public decimal OHLC4 => (Open + High + Low + Close) / 4;
        
        public decimal HLC3 => (High + Low + Close) / 3;
        
        public decimal HL2 => (High + Low) / 2;

        public bool isValid
        {
            get
            {
                var allPricesGreaterThanZero = Open > 0 && High > 0 && Low > 0 && Close > 0;
                var highGreaterThanOtherPrices = High >= Open && High >= Low && High >= Close;
                var lowLessThanOtherPrices = Low <= Open && Low <= High && Low <= Close;
                return allPricesGreaterThanZero && highGreaterThanOtherPrices && lowLessThanOtherPrices;
            }
        }

        public NumericRange RangeBetweenBodyAndHigh
        {
            get
            {
                if (Close > Open)
                {
                    return new NumericRange(Close, High);
                }
                else
                {
                    return new NumericRange(Open, High);
                }
            }
        }

        public NumericRange RangeBetweenBodyAndLow
        {
            get
            {
                if (Close > Open)
                {
                    return new NumericRange(Open, Low);
                }
                else
                {
                    return new NumericRange(Close, Low);
                }
            }
        }

        public bool IsGreenCandle => Close > Open;

        public bool IsRedCandle => Close < Open;

        public bool IsDojiCandle => Close == Open;

        public bool IsContentCandle
        {
            get
            {
                if (High - Low == 0)
                {
                    return false;
                }
                return Math.Abs(Close - Open) / Math.Abs(High - Low) > 0.6m;
            }
        }

        public NumericRange TopHalfOfCandle => new NumericRange((High + Low) / 2, High);

        public NumericRange BottomHalfOfCandle => new NumericRange(Low, (High + Low) / 2);

        public NumericRange CandleRange => new NumericRange(Low, High);

        public NumericRange CenterPoint => new NumericRange((High + Low) / 2, (High + Low) / 2);

        public override bool Equals(object? obj)
        {
            if (obj is Price price)
            {
                return Date == price.Date
                    && Open == price.Open
                    && High == price.High
                    && Low == price.Low
                    && Close == price.Close
                    && Volume == price.Volume;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Date, Open, High, Low, Close, Volume);
        }

        public override string ToString()
        {
            return $"{Date:s};O:{Open};H:{High};L:{Low};C:{Close};M:{(Low + High)/2},V:{Volume}";
        }
    }

    public interface IPrice : IQuote
    {
    }
}
