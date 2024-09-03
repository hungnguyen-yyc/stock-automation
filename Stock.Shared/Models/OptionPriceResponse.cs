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
        public ResponseStatus Status { get; set; }

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
        
        public string DateFormatted => Date.ToString("yyyy-MM-dd");

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
        public double? Ask { get; set; }

        [JsonProperty("askSize")]
        public long? AskSize { get; set; }

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
        
        public long VolumeChange { get; set; }
        
        public double VolumeChangePercentage { get; set; }
        
        public string VolumeChangeSummary => $"{Volume} ({VolumeChange} | {VolumeChangePercentage}%)";
        
        public long OpenInterestChange { get; set; }
        
        public double OpenInterestChangePercentage { get; set; }
        
        public string OpenInterestChangeSummary => $"{OpenInterest} ({OpenInterestChange} | {OpenInterestChangePercentage}%)";
        
        public long TradesChange { get; set; }
        
        public double TradesChangePercentage { get; set; }
        
        public string TradesChangeSummary => $"{Trades} ({TradesChange} | {TradesChangePercentage}%)";

        public double VolumeOpenInterestRatio
        {
            get
            {
                var volumeValue = Volume.HasValue ? Volume.Value : 0.0;
                var openInterestValue = OpenInterest.HasValue ? OpenInterest.Value : 0.0;
                var volumeOpenInterestRatio = volumeValue == 0 || openInterestValue == 0 ? 0 :  Math.Round((volumeValue / openInterestValue), 2);
                return volumeOpenInterestRatio;
            }
        }

        public override string ToString()
        {
            return $"{Symbol}|{Date:yyyy-MM-dd}|O: {Open}|H: {High}|L: {Low}|C: {Close}";
        }

        public string ToStringV2()
        {
            var volumeValue = Volume.HasValue ? Volume.Value : 0.0;
            var openInterestValue = OpenInterest.HasValue ? OpenInterest.Value : 0.0;
            var volumeOpenInterestRatio = volumeValue == 0 || openInterestValue == 0 ? 0 :  Math.Round((volumeValue / openInterestValue), 2);
            return $"{Symbol}|{Date:yyyy-MM-dd}|V: {Volume}|OI: {OpenInterest}|VOIR: {volumeOpenInterestRatio}|Trade: {Trades}|V: {Volatility}";
        }
    }
    
    public class OptionPriceList : List<OptionPrice>
    {
        public OptionPriceList() : this(new List<OptionPrice>())
        {
        }
        
        public OptionPriceList(IEnumerable<OptionPrice> collection) : base(collection)
        {
            CalculateTradesChange();
            CalculateTradesChangePercentage();
            CalculateVolumeChange();
            CalculateVolumeChangePercentage();
            CalculateOpenInterestChange();
            CalculateOpenInterestChangePercentage();
        }
        
        // calculate volume change
        public void CalculateVolumeChange()
        {
            for (var i = 1; i < Count; i++)
            {
                var thisVolume = this[i].Volume.HasValue ? this[i].Volume.Value : 0;
                var prevVolume = this[i - 1].Volume.HasValue ? this[i - 1].Volume.Value : 1;
                this[i].VolumeChange = thisVolume - prevVolume;
            }
        }
        
        // calculate volume change percentage
        public void CalculateVolumeChangePercentage()
        {
            for (var i = 1; i < Count; i++)
            {
                var thisVolume = this[i].Volume.HasValue ? this[i].Volume.Value : 0;
                var prevVolume = this[i - 1].Volume.HasValue ? this[i - 1].Volume.Value : 1;
                this[i].VolumeChangePercentage = prevVolume == 0 ? 0 : Math.Round((((thisVolume - prevVolume) / (double)prevVolume) * 100), 2);
            }
        }
        
        // calculate open interest change
        public void CalculateOpenInterestChange()
        {
            for (var i = 1; i < Count; i++)
            {
                var thisOpenInterest = this[i].OpenInterest.HasValue ? this[i].OpenInterest.Value : 0;
                var prevOpenInterest = this[i - 1].OpenInterest.HasValue ? this[i - 1].OpenInterest.Value : 1;
                this[i].OpenInterestChange = thisOpenInterest - prevOpenInterest;
            }
        }
        
        // calculate open interest change percentage
        public void CalculateOpenInterestChangePercentage()
        {
            for (var i = 1; i < Count; i++)
            {
                var thisOpenInterest = this[i].OpenInterest.HasValue ? this[i].OpenInterest.Value : 0;
                var prevOpenInterest = this[i - 1].OpenInterest.HasValue ? this[i - 1].OpenInterest.Value : 1;
                this[i].OpenInterestChangePercentage = prevOpenInterest == 0 ? 0 : Math.Round((((thisOpenInterest - prevOpenInterest) / (double)prevOpenInterest) * 100), 2);
            }
        }
        
        // calculate trades change
        public void CalculateTradesChange()
        {
            for (var i = 1; i < Count; i++)
            {
                var thisTrades = this[i].Trades.HasValue ? this[i].Trades.Value : 0;
                var prevTrades = this[i - 1].Trades.HasValue ? this[i - 1].Trades.Value : 1;
                this[i].TradesChange = thisTrades - prevTrades;
            }
        }
        
        // calculate trades change percentage
        public void CalculateTradesChangePercentage()
        {
            for (var i = 1; i < Count; i++)
            {
                var thisTrades = this[i].Trades.HasValue ? this[i].Trades.Value : 0;
                var prevTrades = this[i - 1].Trades.HasValue ? this[i - 1].Trades.Value : 1;
                this[i].TradesChangePercentage = prevTrades == 0 ? 0 : Math.Round((((thisTrades - prevTrades) / (double)prevTrades) * 100), 2);
            }
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var optionPrice in this)
            {
                sb.AppendLine(optionPrice.ToString());
            }
            return sb.ToString();
        }

        public string ToStringV2()
        {
            var sb = new StringBuilder();
            foreach (var optionPrice in this)
            {
                sb.AppendLine(optionPrice.ToStringV2());
            }
            return sb.ToString();
        }
    }
}
