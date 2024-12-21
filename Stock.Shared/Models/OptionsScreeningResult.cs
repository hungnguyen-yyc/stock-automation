using Newtonsoft.Json;

namespace Stock.Shared.Models;

public class OptionsScreeningResult
{
    [JsonProperty("underlyingSymbol")]
    public string UnderlyingSymbol { get; set; }

    [JsonProperty("instrumentType")]
    public string InstrumentType { get; set; }

    [JsonProperty("exchange")]
    public string Exchange { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("strike")]
    public decimal Strike { get; set; }

    [JsonProperty("expirationDate")]
    public DateTimeOffset ExpirationDate { get; set; }
        
    public string ExpirationDateFormatted => ExpirationDate.ToString("yyyy-MM-dd");

    [JsonProperty("lastPrice")]
    public decimal LastPrice { get; set; }

    [JsonProperty("optionPrice")]
    public decimal OptionPrice { get; set; }

    [JsonProperty("optionNetChange")]
    public decimal OptionNetChange { get; set; }

    [JsonProperty("tradeTime")]
    public DateTimeOffset TradeTime { get; set; }
        
    public string TradeTimeFormatted => TradeTime.ToString("yyyy-MM-dd HH:mm:ss");

    [JsonProperty("delta")]
    public decimal Delta { get; set; }

    public string DeltaFormatted => $"{Delta:P2}";

    [JsonProperty("volume")]
    public double Volume { get; set; }

    [JsonProperty("openInterest")]
    [JsonConverter(typeof(ParseStringConverter))]
    public long OpenInterest { get; set; }

    [JsonProperty("volumeOpenInterestRatio")]
    public decimal VolumeOpenInterestRatio { get; set; }
        
    [JsonProperty("volatility")]
    public decimal Volatility { get; set; }
        
    public double OpenInterestPercentageChange { get; set; }
}