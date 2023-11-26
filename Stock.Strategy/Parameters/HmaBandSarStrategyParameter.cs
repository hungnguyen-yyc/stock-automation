using Stock.Shared.Models.Parameters;

namespace Stock.Strategies.Parameters
{
    public class HmaBandSarStrategyParameter : IStrategyParameter
    {
        SarParameter Sar { get; set; }
    }
}
