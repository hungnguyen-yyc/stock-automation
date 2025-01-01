﻿using Stock.Data;
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
using Stock.Strategies.Trend;
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
        var immediateSwingLowAndSwingPointStrategy = new ImmediateSwingLowAndSwingPointStrategy();
        var swingPointsLiveTradingHighTimeframesStrategy = new SwingPointsLiveTradingHighTimeframesStrategy();
        immediateSwingLowAndSwingPointStrategy.AlertCreated += StrategyEntryAlertCreated;
        
        var cryptoStrategyMap = new Dictionary<CryptoToTradeEnum, ICryptoStrategy>();
        cryptoStrategyMap.Add(CryptoToTradeEnum.Btc, immediateSwingLowAndSwingPointStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Eth, immediateSwingLowAndSwingPointStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Shib, immediateSwingLowAndSwingPointStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Sol, immediateSwingLowAndSwingPointStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Doge, immediateSwingLowAndSwingPointStrategy);
        cryptoStrategyMap.Add(CryptoToTradeEnum.Sui, immediateSwingLowAndSwingPointStrategy);

        var tradingService = TradingServiceInitializer.Init();
        foreach (var cryptoStrategy in cryptoStrategyMap)
        {
            tradingService.AddStrategy(cryptoStrategy.Key, cryptoStrategy.Value);
        }
        
        
        immediateSwingLowStrategy.EntryAlertCreated += StrategyEntryAlertCreated;

        swingPointsLiveTradingHighTimeframesStrategy.EntryAlertCreated += StrategyEntryAlertCreated;
        swingPointsLiveTradingHighTimeframesStrategy.TrendLineCreated += Strategy_TrendLineCreated;
        swingPointsLiveTradingHighTimeframesStrategy.PivotLevelCreated += Strategy_PivotLevelCreated;
            
        foreach (var timeframe in timeframes)
        {
            foreach (var barcharCrypto in tickers)
            {
                try
                {
                    var swingPointStrategyParameter = SwingPointParametersProvider.GetSwingPointStrategyParameter(barcharCrypto, timeframe);
                    
                    IReadOnlyCollection<Price> hour1Prices = await _repo.GetStockDataForHighTimeframesAsc(barcharCrypto, timeframe, DateTime.Now.AddMonths(-3), DateTime.Now.AddDays(1));
                    IReadOnlyCollection<Price> dailyPrices = await _repo.GetStockDataForHighTimeframesAsc(barcharCrypto, Timeframe.Daily, DateTime.Now.AddYears(-3), DateTime.Now.AddDays(1));
                        
                    var priceToStartTesting = hour1Prices.First(x => x.Date >= DateTime.Now.AddMonths(-2));
                        
                    var priceIndex = 0;
                    for (var i = 0; i < hour1Prices.Count; i++)
                    {
                        var price = hour1Prices.ElementAt(i);
                        if (price.Date == priceToStartTesting.Date)
                        {
                            priceIndex = i;
                            break;
                        }
                    }
                        
                    for (int i = priceIndex; i < hour1Prices.Count; i++)
                    {
                        //_tickerAndPrices[barcharCrypto] = hour1Prices.Take(i).ToList();
                        //UpdateFilteredTrendLines(barcharCrypto);
                        var hmaEmaStrategyParameter = new HmaEmaPriceStrategyParameter
                        {
                            Timeframe = timeframe,
                            HmaPeriod = 50
                        };
                        
                        var last10Prices = hour1Prices.TakeLast(10).ToList();
                        var lastPrice = last10Prices.Last();
                        var channel = new ChannelV2(last10Prices.Take(9).ToList());
                        
                        await Task.Run(() =>
                        {
                            var entryParam = ImmediateSwingLowParameterProvider.GetEntryParameter(barcharCrypto);
                            var exitParam = ImmediateSwingLowParameterProvider.GetExitParameter(barcharCrypto);
                            var crypto = CryptosToTrade.BARCHART_CRYPTO_MAP[barcharCrypto];
                            var strategy = cryptoStrategyMap[crypto];
                            
                            
                        
                            swingPointsLiveTradingHighTimeframesStrategy.CheckForTopBottomTouch(barcharCrypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
                            /*if (strategy is ImmediateSwingLowStrategy)
                            {
                                immediateSwingLowStrategy.CheckForBullishEntry(crypto, hour1Prices.Take(i).ToList(), entryParam);

                                exitParam.StopLoss = await tradingService.GetStopLossPrice(crypto) ?? Decimal.MinValue;
                                exitParam.TakeProfit = await tradingService.GetTakeProfitPrice(crypto) ?? Decimal.MaxValue;
                                immediateSwingLowStrategy.CheckForBullishExit(crypto, hour1Prices.Take(i).ToList(), exitParam);
                            }
                            else if (strategy is ImmediateSwingLowAndSwingPointStrategy)
                            {
                                var param =
                                    new ImmediateSwingLowAndSwingPointStrategyExitParameter
                                    {
                                        StopLoss = await tradingService.GetStopLossPrice(crypto) ?? Decimal.MinValue,
                                        NumberOfCandlesticksToLookBack = 5,
                                        Timeframe = timeframe
                                    };
                                cryptoStrategyMap[crypto].CheckForBullishEntry(crypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
                                cryptoStrategyMap[crypto].CheckForBullishExit(crypto, hour1Prices.Take(i).ToList(), param);
                            }
                            else
                            {
                                cryptoStrategyMap[crypto].CheckForBullishEntry(crypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
                                cryptoStrategyMap[crypto].CheckForBullishExit(crypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
                            }*/
                            
                            // await cryptoHmaEmaStrategy.Run(ticker, _repo, hmaEmaStrategyParameter);
                            
                            // swingPointsLiveTradingHighTimeframesStrategy.CheckForTopBottomTouch(barcharCrypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
                            //_strategy.CheckForTouchingDownTrendLine(barcharCrypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
                            //_strategy.CheckForTouchingUpTrendLine(barcharCrypto, hour1Prices.Take(i).ToList(), swingPointStrategyParameter);
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