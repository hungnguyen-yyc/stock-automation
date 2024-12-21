namespace Stock.Shared.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public partial class OptionsScreeningResponse
    {
        [JsonProperty("status")]
        public ResponseStatus Status { get; set; }

        [JsonProperty("results")]
        public List<OptionsScreeningResult> Results { get; set; }
    }

    public partial class OptionsScreeningResponse
    {
        public static OptionsScreeningResponse FromJson(string json) => JsonConvert.DeserializeObject<OptionsScreeningResponse>(json, Stock.Shared.Models.Converter.Settings);
    }
}
