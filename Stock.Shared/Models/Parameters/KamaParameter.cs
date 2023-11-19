using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Shared.Models.Parameters
{
    public class KamaParameter
    {
        public int KamaPeriod { get; set; }
        public int KamaFastPeriod { get; set; }
        public int KamaSlowPeriod { get; set; }
    }
}
