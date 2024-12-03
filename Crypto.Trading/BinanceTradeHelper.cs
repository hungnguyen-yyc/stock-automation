using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot;
using Serilog;
using Stock.Shared;

namespace Stock.Trading;

internal class BinanceTradeHelper
{
    private readonly IBinanceRestClient _binanceClient;
    private readonly ILogger _logger;

    public BinanceTradeHelper(IBinanceRestClient binanceClient, ILogger logger)
    {
        _binanceClient = binanceClient;
        _logger = logger;
    }
    
    internal async Task<BinanceOrder?> CreateBinanceBuyOrder(CryptoToTradeEnum cryptoEnum, decimal tradeLimit)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoEnum];
        var binanceCrypto = CryptosToTrade.CryptoEnumToBinanceName[cryptoEnum];
        var binanceBalanceResult = await _binanceClient.SpotApi.Account.GetBalancesAsync();
        var binanceBalance = binanceBalanceResult.Data.FirstOrDefault(x => x.Asset == binanceCrypto);
        BinanceOrder? binanceOrder = null;
            
        // if there is already an open position, do nothing
        if (binanceBalance != null)
        {
            return binanceOrder;
        }
        
        var usdtBalance = await GetUsdtBalance();
        if (usdtBalance < tradeLimit)
        {
            _logger.Information($"Not enough balance to open a position for {cryptoName}");
        }
        else
        {
            var currentPriceResult = await _binanceClient.SpotApi.ExchangeData.GetPriceAsync(cryptoName);
            if (!currentPriceResult.Success)
            {
                _logger.Error($"Error getting current price for {cryptoName}: {currentPriceResult.Error}");
            }
            else
            {
                var currentPrice = currentPriceResult.Data.Price;
                var quantity = usdtBalance / currentPrice;
                quantity = Math.Floor(quantity);
                binanceOrder = await PlaceOrder(cryptoName, currentPrice, quantity, OrderSide.Buy);
            }
        }
        return binanceOrder;
    }
    
    internal async Task<BinanceOrder?> CreateBinanceSellOrder(CryptoToTradeEnum cryptoEnum)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoEnum];
        var binanceCrypto = CryptosToTrade.CryptoEnumToBinanceName[cryptoEnum];
        var binanceBalanceResult = await _binanceClient.SpotApi.Account.GetBalancesAsync();
        var binanceBalance = binanceBalanceResult.Data.FirstOrDefault(x => x.Asset == binanceCrypto);
        BinanceOrder? binanceOrder = null;
            
        // if there is no open position, do nothing
        if (binanceBalance == null)
        {
            return binanceOrder;
        }
        
        var currentPriceResult = await _binanceClient.SpotApi.ExchangeData.GetPriceAsync(cryptoName);
        if (!currentPriceResult.Success)
        {
            _logger.Error($"Error getting current price for {cryptoName}: {currentPriceResult.Error}");
        }
        else
        {
            var currentPrice = currentPriceResult.Data.Price;
            var total = Math.Floor(binanceBalance.Total);
            binanceOrder = await PlaceOrder(cryptoName, currentPrice, total, OrderSide.Sell);
        }
        return binanceOrder;
    }
    
    private async Task<BinanceOrder?> PlaceOrder(string ticker, decimal price, decimal quantity, OrderSide side)
    {
        var orderSide = side == OrderSide.Buy ? "Buy" : "Sell";
        var orderResult = await _binanceClient.SpotApi.Trading.PlaceOrderAsync(
            ticker, 
            side, 
            SpotOrderType.Limit, 
            quantity: quantity,
            price: price,
            timeInForce: TimeInForce.GoodTillCanceled);
                
        if (!orderResult.Success)
        {
            _logger.Error($"Error placing {orderSide} order: {orderResult.Error}");
            return null;
        }

        var orderStatus = orderResult.Data.Status;
        BinanceOrder? binanceOrder = null;
        var tries = 0;
        
        while (orderStatus != OrderStatus.Filled
               && orderStatus != OrderStatus.Canceled
               && orderStatus != OrderStatus.Rejected
               && orderStatus != OrderStatus.Expired)
        {
            Thread.Sleep(5000);
            var binanceOrderResult = await _binanceClient.SpotApi.Trading.GetOrderAsync(ticker, orderResult.Data.Id);
            
            if (!binanceOrderResult.Success && tries < 5)
            {
                _logger.Error($"Error getting {orderSide} order: {binanceOrderResult.Error}");
                orderStatus = OrderStatus.PartiallyFilled;
                tries++;
                await Task.Delay(5000);
                continue;
            }
            
            binanceOrder = binanceOrderResult.Data;
            orderStatus = binanceOrder.Status;
            _logger.Information($"{orderSide} order {binanceOrder.Id} status: {orderStatus}. Remaining: {binanceOrder.Quantity - binanceOrder.QuantityFilled}. Refreshing in 5 seconds...");
        }
                
        _logger.Information($"{orderSide} order placed: {orderStatus} - {orderResult.Data.Id}");
        return binanceOrder;
    }

    private async Task<decimal> GetUsdtBalance()
    {
        var balanceResult = await _binanceClient.SpotApi.Account.GetBalancesAsync();
        if (!balanceResult.Success)
        {
            _logger.Error($"Error getting balance from Binance: {balanceResult.Error}");
            return 0;
        }
        
        var balance = balanceResult.Data.FirstOrDefault(x => x.Asset == "USDT")?.Total ?? 0;
        return balance;
    }
}