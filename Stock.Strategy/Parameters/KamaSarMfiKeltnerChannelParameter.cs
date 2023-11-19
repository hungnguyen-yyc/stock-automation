using Stock.Shared.Models.Parameters;

namespace Stock.Strategies.Parameters
{
    public class KamaSarMfiKeltnerChannelParameter
    {
        public KamaParameter Kama14Parameter { get; set; }
        public KamaParameter Kama75Parameter { get; set; }
        public SarParameter SarParameter { get; set; }
        public MfiParameter MfiParameter { get; set; }
        public KeltnerParameter KeltnerParameter { get; set; }
    }
}
