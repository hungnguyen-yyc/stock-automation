namespace Stock.Shared.Models.Parameters
{
    public class StochRsiParameter
    {
        public int StochPeriod { get; set; }
        public int RsiPeriod { get; set; }
        public int StochRsiSignalPeriod { get; set; }
        public int StochRsiSmoothPeriod { get; set; }
    }
}
