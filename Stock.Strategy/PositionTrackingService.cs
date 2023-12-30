using IBApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies
{
    public class PositionTrackingService
    {

    }

    public class OpenedPosition
    {
        public OpenedPosition(string account, Contract contract, decimal pos, double avgCost)
        {
            Account = account;
            Contract = contract;
            Position = pos;
            AverageCost = avgCost;
        }

        public string Account { get; set; }

        public Contract Contract { get; set; }

        public decimal Position { get; set; }

        public double AverageCost { get; set; }
    }
}
