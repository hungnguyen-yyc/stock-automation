using Newtonsoft.Json;

namespace Stock.Shared.Models;

public class EquityOption
{
    [JsonProperty("underlying_symbol")]
    public string UnderlyingSymbol { get; set; }

    [JsonProperty("symbol")]
    public string Symbol { get; set; }

    [JsonProperty("exchange")]
    public string Exchange { get; set; }

    [JsonProperty("type")]
    public TypeEnum Type { get; set; }

    [JsonProperty("strike")]
    public double Strike { get; set; }

    [JsonProperty("expirationDate")]
    public DateTimeOffset ExpirationDate { get; set; }

    [JsonProperty("expirationType")]
    public string ExpirationType { get; set; }

    [JsonProperty("date"), JsonConverter(typeof(DateUnionConverter))]
    public DateUnion Date { get; set; }

    [JsonProperty("volatility")]
    public double Volatility { get; set; }

    [JsonProperty("delta")]
    public double Delta { get; set; }

    [JsonProperty("gamma")]
    public double Gamma { get; set; }

    [JsonProperty("theta")]
    public double Theta { get; set; }

    [JsonProperty("vega")]
    public double Vega { get; set; }

    [JsonProperty("rho")]
    public double Rho { get; set; }

    [JsonProperty("bid")]
    public double Bid { get; set; }

    [JsonProperty("bidSize")]
    public long BidSize { get; set; }

    [JsonProperty("ask")]
    public double Ask { get; set; }

    [JsonProperty("askSize")]
    public long AskSize { get; set; }

    [JsonProperty("open")]
    public double Open { get; set; }

    [JsonProperty("high")]
    public double High { get; set; }

    [JsonProperty("low")]
    public double Low { get; set; }

    [JsonProperty("last")]
    public double Last { get; set; }

    [JsonProperty("previous")]
    public double Previous { get; set; }

    [JsonProperty("change")]
    public double Change { get; set; }

    [JsonProperty("percentChange")]
    public double PercentChange { get; set; }

    [JsonProperty("premium")]
    public object Premium { get; set; }

    [JsonProperty("volume")]
    public long Volume { get; set; }

    [JsonProperty("openInterest")]
    public long OpenInterest { get; set; }
}