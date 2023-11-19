namespace Stock.Shared.Models.Parameters
{
    public class MfiParameter
    {
        public int MfiPeriod { get; set; }
        public double UpperLimit { get; set; }
        public double LowerLimit { get; set; }
        public double MiddleLimit { get; set; }
    }
}
