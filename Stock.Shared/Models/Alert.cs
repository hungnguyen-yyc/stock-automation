using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Shared.Models
{
    public class SavedAlert : Alert
    {
        public int Id { get; set; }
    }

    public class Alert
    {
        public string Ticker { get; set; }
        public Timeframe Timeframe { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; }
        public string Strategy { get; set; }
        public decimal PriceClosed { get; set; }
        public OrderPosition OrderPosition { get; set; }
        public PositionAction PositionAction { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            } 
            else
            {
                var alert = (Alert)obj;
                return Ticker == alert.Ticker
                    && Timeframe == alert.Timeframe
                    && CreatedAt == alert.CreatedAt
                    && Message == alert.Message
                    && Strategy == alert.Strategy
                    && OrderPosition == alert.OrderPosition
                    && PositionAction == alert.PositionAction;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ticker, Timeframe, CreatedAt, Message, Strategy, OrderPosition, PositionAction);
        }
    }

    public class TopNBottomStrategyAlert : Alert
    {
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal ATR { get; set; }
        public bool IsVolumeCheck { get; set; }
        public bool IsCandleBodyCheck { get; set; }
    }
}
