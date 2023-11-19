namespace Stock.Shared.Models
{
    public class BackTestResult
    {
        public string Ticker { get; set; }
        public decimal ProfitLoss { get; set; }
        public IList<Trade> Trades { get; set; }
    }
}