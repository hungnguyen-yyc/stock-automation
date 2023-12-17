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
        public OrderType OrderType { get; set; }
        public OrderAction OrderAction { get; set; }

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
                    && OrderType == alert.OrderType
                    && OrderAction == alert.OrderAction;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ticker, Timeframe, CreatedAt, Message, Strategy, OrderType, OrderAction);
        }
    }
}
