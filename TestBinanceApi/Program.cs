using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace TestBinanceApi;

class Program
{
    static void Main(string[] args)
    {
        var collection = new ServiceCollection();
        var apiKey = Environment.GetEnvironmentVariable("BINANCE_API_KEY") ?? throw new Exception("API key not found");
        var apiSecret = Environment.GetEnvironmentVariable("BINANCE_API_SECRET") ?? throw new Exception("API secret not found");
        collection.AddBinance(options =>
        {
            options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
        });
        var provider = collection.BuildServiceProvider();
        var bc = provider.GetRequiredService<IBinanceRestClient>();
        
        Console.Write("Enter GO to continue: ");
        var input = Console.ReadLine();
        input = input?.ToUpper().Trim();
        
        while (input == "GO")
        {
            // get balance
            var accountResult = bc.SpotApi.Account.GetBalancesAsync().Result;
            var accountBalances = accountResult.Data.ToList();
            var usdtBalance = 0m;
            foreach (var balance in accountBalances)
            {
                Console.WriteLine($"{balance.Asset}: {balance.Total}");
                if (balance.Asset == "USDT")
                {
                    usdtBalance = balance.Total;
                }
            }
            
            Console.Write("\n");
            Console.Write("Enter a ticker: ");
            var ticker = Console.ReadLine();
            ticker = ticker ?? "BTCUSDT";
            ticker = ticker.ToUpper().Trim();
            ticker = ticker.Contains("USDT") ? ticker : ticker + "USDT";

            var lotSizeResult = bc.SpotApi.ExchangeData.GetPriceAsync(ticker).Result;
            Console.WriteLine(lotSizeResult.Success
                ? $"Lot size: {lotSizeResult.Data.Price}"
                : $"Error getting lot size: {lotSizeResult.Error}");

            var priceResult = bc.SpotApi.ExchangeData.GetPriceAsync(ticker).Result;
            var price = priceResult.Data.Price;
            Console.WriteLine($"Price of {ticker}: {price}");
            
            Console.Write("\n");
            Console.WriteLine("Order direction: 1 - Buy, 2 - Sell");
            Console.Write("Enter order direction: ");
            var orderDirection = Console.ReadLine();
            orderDirection = orderDirection?.Trim();
            if (orderDirection != "1" && orderDirection != "2")
            {
                Console.WriteLine("Invalid order direction");
                continue;
            }

            if (orderDirection == "1")
            {
                var maxUsdtAvailable = usdtBalance * 0.95m; // this is to make sure we have enough balance for fees
                var quantity = maxUsdtAvailable / price;
                quantity = Math.Floor(quantity);
                var buyResult = bc.SpotApi.Trading.PlaceOrderAsync(
                    ticker, 
                    OrderSide.Buy, 
                    SpotOrderType.Limit, 
                    quantity: quantity,
                    price: price,
                    timeInForce: TimeInForce.GoodTillCanceled).Result;
                
                if (!buyResult.Success)
                {
                    Console.WriteLine($"Error placing buy order: {buyResult.Error}");
                    continue;
                }

                var orderResult = buyResult.Data.Status;
                
                while (orderResult != OrderStatus.Filled
                       && orderResult != OrderStatus.Canceled
                       && orderResult != OrderStatus.Rejected
                       && orderResult != OrderStatus.Expired)
                {
                    Thread.Sleep(5000);
                    var orderRule = bc.SpotApi.Trading.GetOrderAsync(ticker, buyResult.Data.Id).Result;
                    orderResult = orderRule.Data.Status;
                    Console.WriteLine($"Buy order {orderRule.Data.Id} status: {orderResult}. Remaining: {orderRule.Data.Quantity - orderRule.Data.QuantityFilled}. Refreshing in 5 seconds...");
                }
                
                Console.WriteLine($"Buy order placed: {orderResult} - {buyResult.Data.Id}");
            }
            else
            {
                // find open asset
                var openBalance = accountBalances.FirstOrDefault(x => x.Asset == ticker.Replace("USDT", ""));
                var total = openBalance?.Total ?? 0;
                total = Math.Floor(total);
                var sellResult = bc.SpotApi.Trading.PlaceOrderAsync(
                    ticker, 
                    OrderSide.Sell, 
                    SpotOrderType.Limit, 
                    quantity: total,
                    price: price,
                    timeInForce: TimeInForce.FillOrKill).Result;
                
                if (!sellResult.Success)
                {
                    Console.WriteLine($"Error placing sell order: {sellResult.Error}");
                    continue;
                }
                
                var orderStatus = sellResult.Data.Status;
                while (orderStatus != OrderStatus.Filled
                       && orderStatus != OrderStatus.Canceled
                       && orderStatus != OrderStatus.Rejected
                       && orderStatus != OrderStatus.Expired)
                {
                    Thread.Sleep(5000);
                    var orderRule = bc.SpotApi.Trading.GetOrderAsync(ticker, sellResult.Data.Id).Result;
                    orderStatus = orderRule.Data.Status;
                    Console.WriteLine($"Sell order {orderRule.Data.Id} status: {orderStatus}. Remaining: {orderRule.Data.Quantity - orderRule.Data.QuantityFilled}. Refreshing in 5 seconds...");
                }
                
                Console.WriteLine($"Sell order placed: {orderStatus} - {sellResult.Data.Id}");
            }

            Console.Write("\n");
            Console.Write("Enter GO to continue: ");
            input = Console.ReadLine();
            input = input?.ToUpper().Trim();
        }
    }
}