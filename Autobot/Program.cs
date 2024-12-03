// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Spectre.Console;
using Stock.Data;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Cryptos;
using Stock.Strategies.Parameters;
using Stock.Trading;

var repo = new StockDataRetrievalService();

var immediateSwingLowStrategy = new ImmediateSwingLowStrategy();
var fastKama = new FastKamaIncreaseStrategy();
var kaufmanTouchingStrategy = new PriceTouchKaufmanStrategy();
var temaReversalStrategy = new TEMATrendFollowingStrategy();
        
var cryptoStrategyMap = new Dictionary<CryptoToTradeEnum, ICryptoStrategy>();
/*cryptoStrategyMap.Add(CryptoToTradeEnum.Btc, immediateSwingLowStrategy);
cryptoStrategyMap.Add(CryptoToTradeEnum.Eth, immediateSwingLowStrategy);
cryptoStrategyMap.Add(CryptoToTradeEnum.Shib, immediateSwingLowStrategy);
cryptoStrategyMap.Add(CryptoToTradeEnum.Doge, fastKama);
cryptoStrategyMap.Add(CryptoToTradeEnum.Sui, temaReversalStrategy);*/

cryptoStrategyMap.Add(CryptoToTradeEnum.Sol, kaufmanTouchingStrategy);

var tradingService = TradingServiceInitializer.Init();
foreach (var cryptoStrategy in cryptoStrategyMap)
{
    tradingService.AddStrategy(cryptoStrategy.Key, cryptoStrategy.Value);
}

var logger = LogInitializer.GetLogger();

// Create Spectre table
var table = new Table();
table.ShowRowSeparators = true;
table.Title = new TableTitle("Binance Account Balance");
table.AddColumn(new TableColumn("Asset").Centered());
table.AddColumn(new TableColumn("Avg. Price").Centered());
table.AddColumn(new TableColumn("Cur. Price").Centered());
table.AddColumn(new TableColumn("Total").Centered());
table.AddColumn(new TableColumn("Value in USDT").Centered());
table.AddColumn(new TableColumn("Profit").Centered());

// Set default culture for currency display
CultureInfo ci = new CultureInfo("en-US");
Thread.CurrentThread.CurrentCulture = ci;
Thread.CurrentThread.CurrentUICulture = ci;

await AnsiConsole.Live(table)
    .StartAsync(async ctx =>
    {
        while (true)
        {
            try
            {
                var cryptoAssets = await tradingService.SyncPortfolioWithBinance();
                
                table.Rows.Clear();
                table.Title = new TableTitle("Binance Account Balance\nLast updated at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                foreach (var asset in cryptoAssets)
                {
                    var currentValue = asset.Quantity * asset.CurrentPrice;
                    var profit = currentValue - asset.AveragePrice * asset.Quantity;
                    
                    table.AddRow(
                        asset.Ticker,
                        asset.AveragePrice.ToString("N2"),
                        asset.CurrentPrice.ToString("N2"),
                        asset.Quantity.ToString("N2"),
                        currentValue.ToString("N2"),
                        profit.ToString("N2")
                    );
                }
                
                foreach (var crypto in cryptoStrategyMap.Keys)
                {
                    var param = new HmaEmaPriceStrategyParameter();
                    param.Timeframe = Timeframe.Hour1;
                    
                    var strategy = cryptoStrategyMap[crypto];
                    var barchartTicker = CryptosToTrade.CryptoEnumToBarchartName[crypto];
                    var prices = await repo.GetStockDataForHighTimeframesAsc(barchartTicker, param.Timeframe, DateTime.Now.AddMonths(-3), DateTime.Now.AddDays(1));            
                    strategy.CheckForBullishEntry(crypto, prices, param);
                    strategy.CheckForBullishExit(crypto, prices, param);
                }
                
                ctx.Refresh();
                await Task.Delay(1800000);
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                await Task.Delay(15000);
            }
        }
    });
    