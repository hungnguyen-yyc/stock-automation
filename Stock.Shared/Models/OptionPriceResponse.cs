using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Shared.Models
{
    public class OptionPriceResponse
    {
        [JsonProperty("status")]
        public Status Status { get; set; }

        [JsonProperty("results")]
        public OptionPrice[] OptionPrice { get; set; }

        public static OptionPriceResponse? FromJson(string json) => JsonConvert.DeserializeObject<OptionPriceResponse>(json, new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        });
    }

    public class OptionPrice
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        [JsonProperty("open")]
        public double? Open { get; set; }

        [JsonProperty("high")]
        public double? High { get; set; }

        [JsonProperty("low")]
        public double? Low { get; set; }

        [JsonProperty("close")]
        public double? Close { get; set; }

        [JsonProperty("volume")]
        public long? Volume { get; set; }

        [JsonProperty("openInterest")]
        public long? OpenInterest { get; set; }

        [JsonProperty("trades")]
        public long? Trades { get; set; }

        [JsonProperty("ask")]
        public double Ask { get; set; }

        [JsonProperty("askSize")]
        public long AskSize { get; set; }

        [JsonProperty("bid")]
        public double? Bid { get; set; }

        [JsonProperty("bidSize")]
        public long? BidSize { get; set; }

        [JsonProperty("volatility")]
        public double Volatility { get; set; }

        [JsonProperty("theoretical")]
        public double Theoretical { get; set; }

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
        
        public string GetOptionRecommendation(Option option, decimal underlyingPrice)
        {
            var timeUntilExpiration = option.ExpiryDate - DateTime.Now;
            var daysUntilExpiration = timeUntilExpiration.TotalDays;
            
            var intrinsicValue = option.OptionType == OptionTypeEnum.C.ToString() ? Math.Max(0, underlyingPrice - option.StrikePrice) : Math.Max(0, option.StrikePrice - underlyingPrice);
            var extrinsicValue = (decimal)Ask - intrinsicValue;
            var thetaImpact = Theta * daysUntilExpiration;
            var thetaImpactDecimal = (decimal)thetaImpact;
            var recommendation = "Hold";

            var shouldBuy = thetaImpactDecimal > -extrinsicValue;
            var shouldSell = thetaImpactDecimal < -extrinsicValue;

            if ((option.IsCallOption || option.IsPutOption) && shouldBuy)
            {
                recommendation = "Buy";
            }
            else if ((option.IsCallOption || option.IsPutOption) && shouldSell)
            {
                recommendation = "Sell";
            }
            
            return recommendation;
        }
        
        public string ToString(Option option, decimal underlyingPrice)
        {
            var recommendation = GetOptionRecommendation(option, underlyingPrice);
            return $"{Symbol}|{Date:yyyy-MM-dd}|O: {Open}|H: {High}|L: {Low}|C: {Close}|Recommendation: {recommendation}";
        }

        public override string ToString()
        {
            return $"{Symbol}|{Date:yyyy-MM-dd}|O: {Open}|H: {High}|L: {Low}|C: {Close}";
        }
    }

    public partial class Status
    {
        [JsonProperty("code")]
        public long Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
