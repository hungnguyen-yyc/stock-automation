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

        public bool Intersect(NumericRange range2)
        {
            var intersect = this.Low <= range2.Low && this.High >= range2.High
                || this.Low >= range2.Low && this.High <= range2.High
                || this.Low <= range2.Low && this.High >= range2.Low
                || this.Low <= range2.High && this.High >= range2.High;
            return intersect;
        }

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
        
        public override string ToString()
        {
            var center = (Low + High) / 2;
            return $"{center:F} ({Low:F} - {High:F})";
        }
    }
}
