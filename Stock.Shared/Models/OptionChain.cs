using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace Stock.Shared.Models
{
    public class OptionChain
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("underlying")]
        public Underlying Underlying { get; set; }

        [JsonProperty("options")]
        public Options Options { get; set; }

        public static OptionChain? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<OptionChain>(json, new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
            });
        }
    }

    public class Options
    {
        [JsonProperty("active")]
        public string[] Active { get; set; }

        [JsonProperty("expired")]
        public string[] Expired { get; set; }
    }

    public class Underlying
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ddf")]
        public string Ddf { get; set; }

        [JsonProperty("exchange")]
        public string Exchange { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("symbolType")]
        public long SymbolType { get; set; }

        [JsonProperty("unitcode")]
        public long Unitcode { get; set; }

        [JsonProperty("expired")]
        public bool Expired { get; set; }

        [JsonProperty("hasOptions")]
        public bool HasOptions { get; set; }
    }
}
