using Stock.Shared.Models.Parameters;
using Stock.Strategy;

namespace Stock.Strategies.Parameters
{
    public class KamaSarMfiKeltnerChannelParameter : IStrategyParameter
    {
        public KamaParameter Kama14Parameter { get; set; }
        public KamaParameter Kama75Parameter { get; set; }
        public SarParameter SarParameter { get; set; }
        public MfiParameter MfiParameter { get; set; }
        public KeltnerParameter KeltnerParameter { get; set; }
    }
}
