using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies.Parameters;

namespace Stock.Strategies.Cryptos;

public interface ICryptoStrategy : IStrategy
{
    void CheckForBullishEntry(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter);
        
    void CheckForBullishExit(CryptoToTradeEnum crypto, IReadOnlyCollection<Price> prices, IStrategyParameter parameter);
}