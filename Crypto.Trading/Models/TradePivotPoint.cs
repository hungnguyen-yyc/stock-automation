using Newtonsoft.Json;
using Stock.Shared.Models;

namespace Stock.Trading.Models;

public class BinanceTradePivotPoint
{
    public BinanceTradePivotPoint(long binanceTradeId, TradePivotPoint? tradePivotPoint)
    {
        BinanceTradeId = binanceTradeId;
        TradePivotPoint = tradePivotPoint;
    }

    public TradePivotPoint? TradePivotPoint { get; set; }

    public long BinanceTradeId { get; set; }
    
    public static BinanceTradePivotPoint? FromJson(long binanceTradeId, string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        
        var pivotPoint = TradePivotPoint.Deserialize(json);
        if (pivotPoint == null)
        {
            return null;
        }
        
        return new BinanceTradePivotPoint(binanceTradeId, pivotPoint);
    }
}


public class TradePivotPoint
{
    public TradePivotPoint(string ticker, decimal stopLoss, Price level, Price trendLineStart, Price trendLineEnd)
    {
        Ticker = ticker;
        StopLoss = stopLoss;
        Level = level;
        TrendLineStart = trendLineStart;
        TrendLineEnd = trendLineEnd;
    }
    
    public TradePivotPoint()
    {
    }
    
    [JsonProperty("ticker")]
    public string Ticker { get; set; }
    
    [JsonProperty("stopLoss")]
    public decimal StopLoss { get; set; }
    
    [JsonProperty("level")]
    public Price Level { get; set; }
    
    [JsonProperty("trendLineStart")]
    public Price TrendLineStart { get; set; }
    
    [JsonProperty("trendLineEnd")]
    public Price TrendLineEnd { get; set; }
    
    public string Serialize()
    {
        return JsonConvert.SerializeObject(this);
    }
    
    public static TradePivotPoint? Deserialize(string json)
    {
        return JsonConvert.DeserializeObject<TradePivotPoint>(json);
    }
}