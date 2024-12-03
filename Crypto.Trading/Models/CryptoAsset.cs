namespace Stock.Trading.Models;

public class CryptoAsset : InHouseOpenPosition
{
    public CryptoAsset(string ticker, decimal averagePrice, decimal quantity, DateTimeOffset entryTime) : base(ticker, averagePrice, quantity, entryTime)
    {
        CurrentPrice = averagePrice;
    }

    public decimal CurrentPrice { get; set; }
}