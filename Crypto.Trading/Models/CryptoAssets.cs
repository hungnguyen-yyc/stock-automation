namespace Stock.Trading.Models;

public class CryptoAssets : List<CryptoAsset>
{
    public decimal TotalValue => this.Sum(x => x.Quantity * x.AveragePrice);
    
    public decimal USDT => this
        .Where(x => x.Ticker.Equals("USDT", StringComparison.InvariantCultureIgnoreCase))
        .Sum(x => x.Quantity);
}