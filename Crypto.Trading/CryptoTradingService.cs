using System.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Serilog;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Cryptos;
using Stock.Strategies.EventArgs;
using Stock.Trading.Models;

namespace Stock.Trading;

internal class CryptoTradingService : ITradingService
{
    private const decimal TradeLimit  = 10;
    private readonly ILogger _logger;
    private readonly SqliteDbRepository _dbRepository;
    private readonly IBinanceRestClient _binanceClient;
    private readonly bool _isBackTest;
    private IDictionary<CryptoToTradeEnum, IStrategy> _cryptoStrategyMap;
    private readonly BinanceTradeHelper _binanceTradeHelper;
    private readonly LocalTradeRecordHelper _localTradeRecordHelper;

    public CryptoTradingService(ILogger logger, SqliteDbRepository dbRepository, IBinanceRestClient binanceClient, bool isBackTest = true)
    {
        _logger = logger;
        _dbRepository = dbRepository;
        _binanceClient = binanceClient;
        _isBackTest = isBackTest;
        _cryptoStrategyMap = new Dictionary<CryptoToTradeEnum, IStrategy>();
        
        _binanceTradeHelper = new BinanceTradeHelper(binanceClient, logger);
        _localTradeRecordHelper = new LocalTradeRecordHelper(dbRepository, logger);
    }
    
    public void AddStrategy(CryptoToTradeEnum ticker, ICryptoStrategy strategy)
    {
        if (!_cryptoStrategyMap.TryAdd(ticker, strategy))
        {
            var tickerName = CryptosToTrade.CryptoEnumToName[ticker];
            _logger.Warning($"Strategy for {tickerName} already exists.");
            return;
        }

        strategy.EntryAlertCreated += async (sender, e) => await StrategyEntryAlertCreated(sender, e);
        strategy.ExitAlertCreated += async (sender, e) => await StrategyExitAlertCreated(sender, e);;
    }
    
    public async Task<decimal?> GetTakeProfitPrice(CryptoToTradeEnum ticker)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[ticker];
        var currentPriceResult = (await _dbRepository.GetOpenPosition(cryptoName))?.TakeProfitPrice;
        return currentPriceResult;
    } 
    
    public async Task<decimal?> GetStopLossPrice(CryptoToTradeEnum ticker)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[ticker];
        var currentPriceResult = (await _dbRepository.GetOpenPosition(cryptoName))?.StopLossPrice;
        return currentPriceResult;
    }

    public async Task<CryptoAssets> SyncPortfolioWithBinance()
    {
        var balanceResult = await _binanceClient.SpotApi.Account.GetBalancesAsync();
        if (!balanceResult.Success)
        {
            _logger.Error($"Error getting balance from Binance: {balanceResult.Error}");
            return new CryptoAssets();
        }
        
        var balancesAsync = balanceResult.Data
            .Select(async x =>
            {
                if (x.Asset == "USDT")
                {
                    return new CryptoAsset(x.Asset, 1, x.Total, DateTime.Now);
                }
                
                if (!CryptosToTrade.CryptoNameToEnum.TryGetValue(x.Asset, out var cryptoEnum))
                {
                    return null;
                }
                
                var binanceAsset = CryptosToTrade.CryptoEnumToBinanceName[cryptoEnum];
                var currentPriceResult = await _binanceClient.SpotApi.ExchangeData.GetPriceAsync(binanceAsset);
                
                if (!currentPriceResult.Success)
                {
                    _logger.Error($"Error getting current price for {x.Asset}: {currentPriceResult.Error}");
                    return null;
                }
                
                var orderDateResult = await _binanceClient.SpotApi.Trading.GetOrdersAsync(binanceAsset);
                if (!orderDateResult.Success)
                {
                    _logger.Error($"Error getting order date for {x.Asset}: {orderDateResult.Error}");
                    return null;
                }

                var totalDepositQuantity = 0.0m;
                var depositResult = await _binanceClient.SpotApi.Account.GetDepositHistoryAsync(x.Asset);
                if (depositResult.Success)
                {
                    totalDepositQuantity = depositResult.Data
                        .Where(x => x.Status == DepositStatus.Success)
                        .Sum(x => x.Quantity);
                }
                
                // Get average of the asset by calculating the average price of all buy orders
                var averagePrice = 0.0m;
                var totalOrderedQuantity = 0.0m;
                for (var i = 0; i < orderDateResult.Data.Count(); i++)
                {
                    var order = orderDateResult.Data.ElementAt(i);
                    if (order is { Side: OrderSide.Buy, Status: OrderStatus.Filled })
                    {
                        averagePrice = (averagePrice * totalOrderedQuantity + order.Price * order.Quantity) / (totalOrderedQuantity + order.Quantity);
                        totalOrderedQuantity += order.Quantity;
                    }
                    else if (order is { Side: OrderSide.Sell, Status: OrderStatus.Filled })
                    {
                        totalOrderedQuantity -= order.Quantity;
                    }
                }
                
                var totalQuantity = totalOrderedQuantity + totalDepositQuantity;
                
                // update asset balance in usdt in db
                var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoEnum];
                await _dbRepository.CreateOrUpdateCryptoBalance(cryptoName, totalQuantity * currentPriceResult.Data.Price);

                var lastFilledBuyOrder = orderDateResult.Data
                    .OrderByDescending(x => x.CreateTime)
                    .FirstOrDefault(x => x is { Side: OrderSide.Buy, Status: OrderStatus.Filled });
                var lastDeposit = depositResult.Data
                    .OrderByDescending(x => x.InsertTime)
                    .FirstOrDefault(x => x.Status == DepositStatus.Success);
                var positionFromDb = (await _dbRepository.GetOpenPosition(binanceAsset))?.EntryTime;
                var entryTime = lastFilledBuyOrder?.CreateTime ?? lastDeposit?.InsertTime ?? positionFromDb ?? DateTime.Now;
                
                var asset = new CryptoAsset(x.Asset, averagePrice, totalQuantity, entryTime)
                    {
                        CurrentPrice = currentPriceResult.Data.Price
                    };

                return asset;
            })
            .ToList();
        
        var balances = await Task.WhenAll(balancesAsync);
        var result = new CryptoAssets();
        result.AddRange(balances.Where(x => x != null).Select(x => x!));

        return result;
    }

    private async Task StrategyExitAlertCreated(object sender, AlertEventArgs e)
    {
        var alert = e.Alert;
        var cryptoName = alert.Ticker;
        var cryptoEnum = CryptosToTrade.CryptoNameToEnum[alert.Ticker];

        if (!_isBackTest)
        {
            var order = await _binanceTradeHelper.CreateBinanceSellOrder(cryptoEnum);
            
            if (order == null)
            {
                _logger.Information($"Failed to sell {cryptoName}");
                return;
            }
            
            _logger.Information($"Sold {cryptoName} at {order.Price} with quantity {order.Quantity}");
            _logger.Information($"Triggered by {e.Alert.Strategy}: {e.Alert.Message}");
        }
        else
        {
            var priceClosed = alert.PriceClosed;
            var closedTime = alert.CreatedAt;
            var message = e.Alert.Message;
            
            await _localTradeRecordHelper.CloseLocalPosition(cryptoEnum, priceClosed, closedTime, message);
        }
    }

    private async Task StrategyEntryAlertCreated(object sender, AlertEventArgs e)
    {
        var alert = e.Alert;
        var cryptoName = alert.Ticker;
        var cryptoEnum = CryptosToTrade.CryptoNameToEnum[alert.Ticker];

        if (!_isBackTest)
        {
            var order = await _binanceTradeHelper.CreateBinanceBuyOrder(cryptoEnum, TradeLimit);
            
            if (order == null)
            {
                _logger.Information($"Failed to buy {cryptoName}");
                return;
            }
            
            _logger.Information($"Bought {cryptoName} at {order.Price} with quantity {order.Quantity}");
            _logger.Information($"Triggered by {e.Alert.Strategy}: {e.Alert.Message}");
        }
        else
        {
            var priceClosed = alert.PriceClosed;
            var stopLoss = alert.StopLoss;
            var takeProfit = alert.TakeProfit;
            var createdTime = alert.CreatedAt;
            var message = e.Alert.Message;
            
            await _localTradeRecordHelper.OpenLocalPosition(cryptoEnum, priceClosed, createdTime, stopLoss, takeProfit, message);
        }
    }
}