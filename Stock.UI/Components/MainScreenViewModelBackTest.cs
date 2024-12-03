using Stock.Data;
using Stock.Data.EventArgs;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Data;
using Microsoft.Toolkit.Uwp.Notifications;
using Stock.Shared.Models.Parameters;
using Stock.Strategies.Cryptos;
using Stock.Strategies.EventArgs;
using Stock.Strategies.Parameters;
using Stock.Trading;

namespace Stock.UI.Components;

public partial class MainScreenViewModel
{
    private async Task RunBackTest()
    {
        var tickers = TickersWithoutAll;
        var timeframes = new[] { Timeframe.Hour1 };
        var immediateSwingLowStrategy = new ImmediateSwingLowStrategy();
        var fastKama = new FastKamaIncreaseStrategy();
        var kaufmanTouchingStrategy = new PriceTouchKaufmanStrategy();
        var temaReversalStrategy = new TEMATrendFollowingStrategy();
        
        var cryptoStrategyMap = new Dictionary<CryptoToTradeEnum, ICryptoStrategy>();
        cryptoStrategyMap.Add(CryptoToTradeEnum.Btc, immediateSwingLowStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Eth, immediateSwingLowStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Shib, immediateSwingLowStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Sol, kaufmanTouchingStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Doge, fastKama);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Sui, temaReversalStrategy);

        var tradingService = TradingServiceInitializer.Init();
        foreach (var cryptoStrategy in cryptoStrategyMap)
        {
            tradingService.AddStrategy(cryptoStrategy.Key, cryptoStrategy.Value);
        }
        
        
        immediateSwingLowStrategy.EntryAlertCreated += StrategyEntryAlertCreated;
            
        foreach (var timeframe in timeframes)
        {
            _strategy.TrendLineCreated -= Strategy_TrendLineCreated;
            _strategy.PivotLevelCreated -= Strategy_PivotLevelCreated;

            _strategy = new SwingPointsLiveTradingHighTimeframesStrategy();

            _strategy.TrendLineCreated += Strategy_TrendLineCreated;
            _strategy.PivotLevelCreated += Strategy_PivotLevelCreated;

            foreach (var barcharCrypto in tickers)
            {
                try
                {
                    var swingPointStrategyParameter = SwingPointParametersProvider.GetSwingPointStrategyParameter(barcharCrypto, timeframe);
                    
                    IReadOnlyCollection<Price> hour1Prices = await _repo.GetStockDataForHighTimeframesAsc(barcharCrypto, timeframe, DateTime.Now.AddMonths(-13), DateTime.Now.AddDays(1));
                    IReadOnlyCollection<Price> dailyPrices = await _repo.GetStockDataForHighTimeframesAsc(barcharCrypto, Timeframe.Daily, DateTime.Now.AddYears(-3), DateTime.Now.AddDays(1));
                        
                    var priceToStartTesting = hour1Prices.First(x => x.Date >= DateTime.Now.AddMonths(-12));
                        
                    var hour4Index = 0;
                    for (int i = 0; i < hour1Prices.Count; i++)
                    {
                        var price = hour1Prices.ElementAt(i);
                        if (price.Date == priceToStartTesting.Date)
                        {
                            hour4Index = i;
                            break;
                        }
                    }
                        
                    for (int i = hour4Index; i < hour1Prices.Count; i++)
                    {
                        _tickerAndPrices[barcharCrypto] = hour1Prices.Take(i).ToList();
                        UpdateFilteredTrendLines(barcharCrypto);
                        var hmaEmaStrategyParameter = new HmaEmaPriceStrategyParameter
                        {
                            Timeframe = timeframe,
                            HmaPeriod = 50
                        };
                        await Task.Run(() =>
                        {
                            var entryParam = ImmediateSwingLowParameterProvider.GetEntryParameter(barcharCrypto);
                            var exitParam = ImmediateSwingLowParameterProvider.GetExitParameter(barcharCrypto);
                            var crypto = CryptosToTrade.BARCHART_CRYPTO_MAP[barcharCrypto];
                            var strategy = cryptoStrategyMap[crypto];
                            if (strategy is ImmediateSwingLowStrategy)
                            {
                                immediateSwingLowStrategy.CheckForBullishEntry(crypto, hour1Prices.Take(i).ToList(), entryParam);

                                exitParam.StopLoss = tradingService.GetStopLossPrice(crypto) ?? Decimal.MinValue;
                                exitParam.TakeProfit = tradingService.GetTakeProfitPrice(crypto) ?? Decimal.MaxValue;
                                immediateSwingLowStrategy.CheckForBullishExit(crypto, hour1Prices.Take(i).ToList(), exitParam);
                            }
                            else
                            {
                                cryptoStrategyMap[crypto].CheckForBullishEntry(crypto, hour1Prices.Take(i).ToList(), entryParam);
                                cryptoStrategyMap[crypto].CheckForBullishExit(crypto, hour1Prices.Take(i).ToList(), entryParam);
                            }
                            
                            // await cryptoHmaEmaStrategy.Run(ticker, _repo, hmaEmaStrategyParameter);
                            //_strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            //_strategy.CheckForTouchingDownTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                            //_strategy.CheckForTouchingUpTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter);
                        });
                    }
                    Logs.Add(new LogEventArg($"Finished running strategy for {barcharCrypto} {timeframe} at {DateTime.Now}"));
                }
                catch (Exception ex)
                {
                    Logs.Add(new LogEventArg(ex.Message));
                }
            }
        }

        Logs.Add(new LogEventArg($"Finished running strategy at {DateTime.Now}"));
    }
}