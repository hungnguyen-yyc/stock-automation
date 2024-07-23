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

        public IReadOnlyCollection<Option> ParsedActiveOptions
        {
            get
            {
                var result = new List<Option>();
                if (Active == null)
                {
                    return result;
                }

                foreach (var optionString in Active)
                {
                    // the option string format is "Ticker|ExpiryDate|StrikePriceOptionType"
                    string[] parts = optionString.Split('|');

                    var strikePriceOptionType = parts[2];

                    var strikePrice = decimal.Parse(strikePriceOptionType.Substring(0, strikePriceOptionType.Length - 1));
                    var optionType = strikePriceOptionType.Substring(strikePriceOptionType.Length - 1);

                    var option = new Option
                    {
                        Ticker = parts[0],
                        ExpiryDate = DateTime.ParseExact(parts[1], "yyyyMMdd", null),
                        StrikePrice = strikePrice,
                        OptionType = optionType
                    };

                    result.Add(option);
                }

                return result;
            }
        }

        public IReadOnlyCollection<Option> ParsedExpiredOptions
        {
            get
            {
                var result = new List<Option>();
                if (Expired == null)
                {
                    return result;
                }

                foreach (var optionString in Expired)
                {
                    // the option string format is "Ticker|ExpiryDate|StrikePriceOptionType"
                    string[] parts = optionString.Split('|');

                    var strikePriceOptionType = parts[2];

                    decimal strikePrice = decimal.Parse(strikePriceOptionType.Substring(0, strikePriceOptionType.Length - 1));
                    var optionType = strikePriceOptionType.Substring(strikePriceOptionType.Length - 1);

                    var option = new Option
                    {
                        Ticker = parts[0],
                        ExpiryDate = DateTime.ParseExact(parts[1], "yyyyMMdd", null),
                        StrikePrice = strikePrice,
                        OptionType = optionType
                    };

                    result.Add(option);
                }

                return result;
            }
        }
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

    public class Option
    {
        public string Ticker { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; }
        
        public bool IsCallOption => OptionType == OptionTypeEnum.C.ToString();
        
        public bool IsPutOption => OptionType == OptionTypeEnum.P.ToString();
    }

    public enum OptionTypeEnum
    {
        C,
        P
    }
}
