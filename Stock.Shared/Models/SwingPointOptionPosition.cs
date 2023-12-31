using IBApi;
using Stock.Shared.Models.IBKR.Messages;

namespace Stock.Shared.Models
{
    public class SwingPointOptionPosition
    {
        public int Id { get; set; }
        public int TickerId { get; set; }
        public string Ticker { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string AccountId { get; set; }
        public DateTime ExpiredOn { get; set; }
        public decimal Strike { get; set; }
        public string OptionRight { get; set; }
        public decimal LevelHigh { get; set; }
        public decimal LevelLow { get; set; }
        public bool IsClosed { get; set; }

        public bool IsSameAs (PositionMessage positionMessage)
        {
            return Ticker == positionMessage.Contract.Symbol
                && ExpiredOn.ToString("yyyyMMdd") == positionMessage.Contract.LastTradeDateOrContractMonth
                && Strike == (decimal)positionMessage.Contract.Strike
                && OptionRight == positionMessage.Contract.Right;
        }
    }

    public class SavedContract : Contract
    {
        public int ContractId { get; set; }
    }
}
