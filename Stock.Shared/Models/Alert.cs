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
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public OrderPosition OrderPosition { get; set; }
        public PositionAction PositionAction { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var alert = (Alert)obj;
            return Ticker == alert.Ticker
                   && Timeframe == alert.Timeframe
                   && CreatedAt == alert.CreatedAt
                   && Message == alert.Message
                   && Strategy == alert.Strategy
                   && OrderPosition == alert.OrderPosition
                   && PositionAction == alert.PositionAction;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ticker, Timeframe, CreatedAt.ToString("s"), Message, Strategy, PriceClosed, OrderPosition, PositionAction);
        }

        public virtual string ToCsvString()
        {
            return $"{Ticker},{Timeframe},{CreatedAt},{Message},{Strategy},{PriceClosed},{OrderPosition},{PositionAction}";
        }
    }

    public class TopNBottomStrategyAlert : Alert
    {
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Center { get; set; }
        public decimal ATR { get; set; }
        
        public override string ToCsvString()
        {
            return $"{Ticker},{Timeframe},{CreatedAt},{Message},{Strategy},{PriceClosed},{OrderPosition},{PositionAction},{High},{Low},{Center},{ATR}";
        }
    }

    public class HighChangeInOpenInterestStrategyAlert : Alert
    {
        public string OptionTicker { get; set; }
    }
}
