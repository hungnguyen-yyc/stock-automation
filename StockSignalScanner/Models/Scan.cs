using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Models
{
    internal class Scan
    {
        public long Id { get; set; }
        public string DateCreated { get; set; }
        public IList<Indicator> Indicators { get; set; }
    }

    internal class Indicator
    {
        public long Id { get; set; }
        public string Name { get; set; }
        IList<IndicatorParameter> Parameters { get; set; }

    }

    internal class IndicatorParameter
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
