namespace Stock.Shared.Models
{
    public class NumericRange
    {
        public NumericRange(decimal low, decimal high)
        {
            Low = low;
            High = high;
        }

        public decimal Low { get; set; }
        public decimal High { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is NumericRange range)
            {
                return Low == range.Low && High == range.High;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Low, High);
        }
    }
}
