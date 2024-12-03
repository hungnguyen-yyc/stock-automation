using Stock.Shared;
using Stock.Strategies;
using Stock.Strategies.Cryptos;
using Stock.Trading.Models;

namespace Stock.Trading;

public interface ITradingService
{
    Task<CryptoAssets> SyncPortfolioWithBinance();
    
    public void AddStrategy(CryptoToTradeEnum crypto, ICryptoStrategy strategy);
    
    public decimal? GetTakeProfitPrice(CryptoToTradeEnum ticker);
    
    public decimal? GetStopLossPrice(CryptoToTradeEnum ticker);
}