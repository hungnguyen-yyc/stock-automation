namespace Stock.Shared.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public partial class EquityOptionsResponse
    {
        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("results")]
        public List<EquityOption> EquityOptions { get; set; }
    }

    public partial class EquityOptionsResponse
    {
        public static EquityOptionsResponse FromJson(string json) => JsonConvert.DeserializeObject<EquityOptionsResponse>(json, Stock.Shared.Models.Converter.Settings);
    }
}
