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

    public void OpenLocalPosition(CryptoToTradeEnum cryptoToTradeEnum, decimal priceClosed, DateTime createdTime,
        decimal? stopLoss,
        decimal? takeProfit, string message)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoToTradeEnum];
        var openPosition = _dbRepository.GetOpenPosition(cryptoName);
        var balance = _dbRepository.GetCryptoBalance(cryptoName);
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

        // TODO: open Binance position
        _dbRepository.AddPosition(
            new InHouseOpenPosition(
                cryptoName,
                priceClosed,
                quantity,
                createdTime,
                stopLoss,
                takeProfit));
        _logger.Information($"Entry signal created: {message}");
    }

    public void CloseLocalPosition(CryptoToTradeEnum cryptoToTradeEnum, decimal priceClosed, DateTime closedTime,
        string message)
    {
        var cryptoName = CryptosToTrade.CryptoEnumToName[cryptoToTradeEnum];
        var positionToClose = _dbRepository.GetOpenPosition(cryptoName);
        if (positionToClose == null)
        {
            return;
        }

        if (!positionToClose.TryClose(priceClosed, closedTime, out var closedPosition))
        {
            return;
        }

        _dbRepository.ClosePosition(closedPosition);
        _dbRepository.UpdateCryptoBalance(closedPosition.OpenPosition.Ticker,
            closedPosition.OpenPosition.Quantity * closedPosition.ExitPrice);
        _logger.Information($"{message}");
    }
}