using Serilog;
using Stock.Shared;
using Stock.Trading.Models;

namespace Stock.Trading;

internal class LocalTradeRecordHelper
{
    private readonly SqliteDbRepository _dbRepository;
    private readonly ILogger _logger;

    public LocalTradeRecordHelper(SqliteDbRepository dbRepository, ILogger logger)
    {
        _dbRepository = dbRepository;
        _logger = logger;
    }

    public async Task OpenLocalPosition(CryptoToTradeEnum cryptoToTradeEnum, decimal priceClosed, DateTime createdTime,
        decimal? stopLoss,
        decimal? takeProfit, string message)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoToTradeEnum];
        var openPosition = await _dbRepository.GetOpenPosition(cryptoName);
        var balance = await _dbRepository.GetCryptoBalance(cryptoName);
        var quantity = balance / priceClosed;
        if (openPosition != null)
        {
            return;
        }

        if (balance < 10)
        {
            _logger.Information($"Not enough balance to open a position for {cryptoName}");
            return;
        }

        await _dbRepository.AddPosition(
            new InHouseOpenPosition(
                cryptoName,
                priceClosed,
                quantity,
                createdTime,
                stopLoss,
                takeProfit));
        _logger.Information($"Entry signal created: {message}");
    }

    public async Task CloseLocalPosition(CryptoToTradeEnum cryptoToTradeEnum, decimal priceClosed, DateTime closedTime,
        string message)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoToTradeEnum];
        var positionToClose = await _dbRepository.GetOpenPosition(cryptoName);
        if (positionToClose == null)
        {
            return;
        }

        if (!positionToClose.TryClose(priceClosed, closedTime, out var closedPosition))
        {
            return;
        }

        await _dbRepository.ClosePosition(closedPosition);
        await _dbRepository.CreateOrUpdateCryptoBalance(closedPosition.OpenPosition.Ticker,
            closedPosition.OpenPosition.Quantity * closedPosition.ExitPrice);
        _logger.Information($"{message}");
    }
}