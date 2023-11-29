namespace Stock.Strategies.Parameters
{
    public class SwingPointStrategyParameter : IStrategyParameter
    {
        public int NumberOfSwingPointsToLookBack { get; set; }
        public int NumberOfCandlesticksToLookBack { get; set; }
        public int NumberOfCandlesticksToSkipAfterSwingPoint { get; set; }
    }
}
