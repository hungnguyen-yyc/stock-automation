using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Stock.Trading;

public class TradingServiceInitializer
{
    public static ITradingService Init()
    {
        #if DEBUG
        var isBackTest = true;
        #else
        var isBackTest = false;
        #endif
        
        var collection = new ServiceCollection();
        var apiKey = Environment.GetEnvironmentVariable("BINANCE_API_KEY") ?? throw new InvalidOperationException("API Key is not set in environment variables.");
        var apiSecret = Environment.GetEnvironmentVariable("BINANCE_API_SECRET") ?? throw new InvalidOperationException("API Secret is not set in environment variables.");
        collection.AddBinance(options =>
        {
            options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
        });
        var provider = collection.BuildServiceProvider();
        var binanceClient = provider.GetRequiredService<IBinanceRestClient>();
        
        var logger = LogInitializer.GetLogger();
        var dbRepository = new SqliteDbRepository(new SqliteDbInitializer(), logger);
        var tradingService = new CryptoTradingService(logger, dbRepository, binanceClient, isBackTest);
        
        return tradingService;
    }
}