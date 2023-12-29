using IBApi;
using Stock.Data;
using Stock.Data.EventArgs;
using Stock.Shared;
using Stock.Shared.Models;
using Stock.Strategies;
using Stock.Strategies.Parameters;
using Stock.UI.IBKR.Client;
using Stock.UI.IBKR.Managers;
using Stock.UI.IBKR.Messages;
using Syncfusion.Windows.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows.Data;
using Order = IBApi.Order;

namespace Stock.UI.Components
{
    public class MainScreenViewModel : INotifyPropertyChanged
    {
        private const string ALL = "All";
        private const string ClientId = "1"; // hardcoded for now, may not needed
        private string _accountId = string.Empty;

        private IBClient _ibClient;
        private EReaderMonitorSignal _signal;

        private readonly StockDataRepository _repo;
        private readonly SwingPointsLiveTradingStrategy _strategy;
        private IReadOnlyCollection<Price> _prices;
        private string _selectedTimeframe;
        private string _selectedTicker;
        private ObservableCollection<Alert> _allAlerts;
        private ObservableCollection<Alert> _filteredAlerts;
        private ObservableCollection<string> _optionChain;
        private ObservableCollection<string> _optionPrice;
        private readonly object _lock = new();
        private AccountManager _accountManager;
        private PnLManager _pnlManager;
        private OrderManager _orderManager;
        private ObservableCollection<CompletedOrderMessage> _completedOrders;
        private ObservableCollection<Tuple<string, string>> _accountSummary;
        private ObservableCollection<PositionMessage> _accountPosition;
        private string _pnL;

        public delegate void IBKRConnectedHandler(bool isConnected);
        public event IBKRConnectedHandler IBKRConnected;

        public MainScreenViewModel(StockDataRepository repo, SwingPointsLiveTradingStrategy strategy)
        {
            _repo = repo;
            _strategy = strategy;
            _prices = new List<Price>();
            _selectedTimeframe = Timeframe.Minute15.ToString();
            _selectedTicker = ALL;
            _allAlerts = new ObservableCollection<Alert>();
            _filteredAlerts = new ObservableCollection<Alert>();
            BindingOperations.EnableCollectionSynchronization(_filteredAlerts, _lock);

            Logs = new ObservableCollection<LogEventArg>();
            BindingOperations.EnableCollectionSynchronization(Logs, _lock);

            _optionChain = new ObservableCollection<string>();
            _optionPrice = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(_optionChain, _lock);
            BindingOperations.EnableCollectionSynchronization(_optionPrice, _lock);

            _signal = new EReaderMonitorSignal();
            _ibClient = new IBClient(_signal);
            _ibClient.NextValidId += OnNextValidId;
            _accountManager = new AccountManager(_ibClient);
            _accountSummary = new ObservableCollection<Tuple<string, string>>();
            _accountPosition = new ObservableCollection<PositionMessage>();
            _accountManager.AccountSummaryReceived += OnAccountSummaryReceived;
            _accountManager.PositionReceived += OnPositionReceived;

            BindingOperations.EnableCollectionSynchronization(_accountSummary, _lock);
            BindingOperations.EnableCollectionSynchronization(_accountPosition, _lock);

            _pnlManager = new PnLManager(_ibClient);
            _pnlManager.PnLReceived += OnPnLReceived;

            _orderManager = new OrderManager(_ibClient);
            _orderManager.CompletedOrderReceived += OnCompletedOrderReceived;

            _completedOrders = new ObservableCollection<CompletedOrderMessage>();
            BindingOperations.EnableCollectionSynchronization(_completedOrders, _lock);

            _repo.LogCreated += (message) =>
            {
                Logs.Add(message);
            };

            Tickers = new ObservableCollection<string>
            {
                ALL
            };

            foreach (var ticker in TickersToTrade.POPULAR_TICKERS)
            {
                Tickers.Add(ticker);
            }

            Timeframes = new ObservableCollection<string>
            {
                ALL,
                Timeframe.Minute15.ToString(),
                Timeframe.Minute30.ToString(),
                Timeframe.Hour1.ToString(),
                Timeframe.Daily.ToString()
            };

            _strategy.AlertCreated += Strategy_AlertCreated;
            StartStrategy();
        }

        private void OnCompletedOrderReceived(List<CompletedOrderMessage> completedOrder)
        {
            _completedOrders.Clear();
            foreach (var order in completedOrder)
            {
                _completedOrders.Add(order);
            }
        }

        public ObservableCollection<CompletedOrderMessage> CompletedOrders
        {
            get => _completedOrders;
            set
            {
                _completedOrders = value;
                OnPropertyChanged(nameof(CompletedOrders));
            }
        }

        public string PnL { 
            get => _pnL;
            private set
            {
                _pnL = value;
                OnPropertyChanged(nameof(PnL));
            }
        }

        private void OnPnLReceived(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL)
        {
            Logs.Add(new LogEventArg("PnL received."));
            PnL = $"Daily PnL: {dailyPnL}, Unrealized PnL: {unrealizedPnL}, Realized PnL: {realizedPnL}";
        }

        private void OnPositionReceived(PositionMessage positionMessage)
        {
            Logs.Add(new LogEventArg("Position received."));
            var existedPosition = _accountPosition.FirstOrDefault(x => 
                x.Account == positionMessage.Account 
                && x.Contract.Symbol == positionMessage.Contract.Symbol
                && x.Contract.Currency == positionMessage.Contract.Currency
                && x.Contract.SecType == positionMessage.Contract.SecType
                && x.Contract.Exchange == positionMessage.Contract.Exchange
                && x.Contract.LastTradeDateOrContractMonth == positionMessage.Contract.LastTradeDateOrContractMonth);

            if (existedPosition != null)
            {
                _accountPosition.Remove(existedPosition);
            }

            _accountPosition.Add(positionMessage);
        }

        private string AccountId
        {
            get => _accountId;
            set
            {
                if (_accountId != value)
                {
                    _accountId = value;
                    _pnlManager.ReqPnL(_accountId, "");
                    OnPropertyChanged(nameof(AccountId));
                }
            }
        }

        private void OnAccountSummaryReceived(AccountSummaryMessage accountSummary)
        {
            Logs.Add(new LogEventArg("Account summary received."));
            var ACCOUNT = "Account";
            var CURRENCY = "Currency";
            var hasAccount = AccountSummary.Any(x => x.Item1 == ACCOUNT);
            if (!hasAccount)
            {
                AccountSummary.Add(new Tuple<string, string>(ACCOUNT, accountSummary.Account));
                _accountId = accountSummary.Account;
            }
            else
            {
                var account = AccountSummary.First(x => x.Item1 == ACCOUNT);
                AccountSummary.Remove(account);
                AccountSummary.Insert(0, new Tuple<string, string>(ACCOUNT, accountSummary.Account));
            }

            var hasCurrency = AccountSummary.Any(x => x.Item1 == CURRENCY);
            if (!hasCurrency)
            {
                AccountSummary.Add(new Tuple<string, string>(CURRENCY, accountSummary.Currency));
            }
            else
            {
                var currency = AccountSummary.First(x => x.Item1 == CURRENCY);
                AccountSummary.Remove(currency);
                AccountSummary.Insert(1, new Tuple<string, string>(CURRENCY, accountSummary.Currency));
            }

            var hasTag = AccountSummary.Any(x => x.Item1 == accountSummary.Tag);
            if (!hasTag)
            {
                AccountSummary.Add(new Tuple<string, string>(accountSummary.Tag, accountSummary.Value));
            }
            else
            {
                var account = AccountSummary.First(x => x.Item1 == accountSummary.Tag);
                AccountSummary.Remove(account);
                AccountSummary.Add(new Tuple<string, string>(accountSummary.Tag, accountSummary.Value));
            }
        }

        public bool IsConnected { get; private set; }
        public ObservableCollection<Tuple<string, string>> AccountSummary { 
            get => _accountSummary; 
            set
            {
                _accountSummary = value;
                OnPropertyChanged(nameof(AccountSummary));
            }
        }

        public ObservableCollection<PositionMessage> Positions
        {
            get => _accountPosition;
            set
            {
                _accountPosition = value;
                OnPropertyChanged(nameof(Positions));
            }
        }

        public void Connect(string host = "127.0.0.1", int port = 7496)
        {
            Logs.Add(new LogEventArg("Connecting to IBKR..."));
            if (!IsConnected)
            {
                if (host == null || host.Equals(""))
                    host = "127.0.0.1";
                try
                {
                    _ibClient.ClientId = int.Parse(ClientId);
                    _ibClient.ClientSocket.eConnect(host, port, _ibClient.ClientId);

                    var reader = new EReader(_ibClient.ClientSocket, _signal);

                    reader.Start();

                    new Thread(() => { while (_ibClient.ClientSocket.IsConnected()) { _signal.waitForSignal(); reader.processMsgs(); } }) { IsBackground = true }.Start();
                }
                catch (Exception)
                {
                    Logs.Add(new LogEventArg("Error code -1. Please check your connection attributes."));
                }
            }
            else
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                IsConnected = false;
                _ibClient.ClientSocket.eDisconnect();
                AccountSummary.Clear();
                Positions.Clear();
                _completedOrders.Clear();
                PnL = "PnL: n/a";
                IBKRConnected?.Invoke(IsConnected);
                Logs.Add(new LogEventArg("Disconnected from IBKR."));
            }
        }

        private void OnNextValidId(ConnectionStatusMessage statusMessage)
        {
            Logs.Add(new LogEventArg($"Connection status: {statusMessage.IsConnected}"));
            IsConnected = statusMessage.IsConnected;
            IBKRConnected?.Invoke(IsConnected);
            GetAccountSummary();
            GetCompletedOrders();
        }

        private void GetAccountSummary()
        {
            Logs.Add(new LogEventArg("Requesting account summary..."));
            _accountManager.RequestAccountSummary();
            _accountManager.RequestPositions();
            Logs.Add(new LogEventArg("Requesting account summary completed."));
        }

        private void GetCompletedOrders()
        {
            Task.Run(async () => { 
                while (IsConnected)
                {
                    Logs.Add(new LogEventArg("Requesting completed orders..."));
                    _ibClient.ClientSocket.reqCompletedOrders(false);
                    Logs.Add(new LogEventArg("Requesting completed orders completed."));
                    await Task.Delay(TimeSpan.FromMinutes(15));
                }
            });
        }

        public IReadOnlyCollection<Price> Prices
        {
            get => _prices;
            set
            {
                _prices = value;
                OnPropertyChanged(nameof(Prices));
            }
        }

        public ObservableCollection<string> OptionChain => _optionChain;

        public ObservableCollection<string> OptionPrice => _optionPrice;

        public ObservableCollection<string> Timeframes { get; }

        public ObservableCollection<string> Tickers { get; }

        public ObservableCollection<LogEventArg> Logs { get; }

        public string SelectedTimeframe
        {
            get { return _selectedTimeframe; }
            set
            {
                if (_selectedTimeframe != value)
                {
                    _selectedTimeframe = value;

                    UpdateFilteredAlerts();

                    OnPropertyChanged(nameof(SelectedTimeframe));
                }
            }
        }

        public string SelectedTicker
        {
            get { return _selectedTicker; }
            set
            {
                if (_selectedTicker != value)
                {
                    _selectedTicker = value;

                    UpdateFilteredAlerts();

                    OnPropertyChanged(nameof(SelectedTicker));
                }
            }
        }

        public ObservableCollection<Alert> Alerts => _filteredAlerts;

        public event PropertyChangedEventHandler? PropertyChanged;

        public async Task GetOptionChain(string ticker, DateTime fromDate, DateTime toDate)
        {
            var optionChain = await _repo.GetOptionChainAsync(ticker, fromDate, toDate);

            if (optionChain == null)
            {
                return;
            }

            _optionChain.Clear();

            var expiredOptions = optionChain.Expired ?? Array.Empty<string>();
            var activeOptions = optionChain.Active ?? Array.Empty<string>();
            var allOptions = expiredOptions.Concat(activeOptions).ToList();

            foreach (var option in allOptions)
            {
                _optionChain.Add(option);
            }

            OnPropertyChanged(nameof(OptionChain));
        }

        public async Task GetOptionPrice(string optionDetailByBarChart)
        {
            var optionPrice = await _repo.GetOptionPriceAsync(optionDetailByBarChart);

            if (optionPrice == null)
            {
                return;
            }

            _optionPrice.Clear();

            foreach (var option in optionPrice)
            {
                _optionPrice.Add(option);
            }

            OnPropertyChanged(nameof(OptionPrice));
        }

        private void UpdateFilteredAlerts()
        {
            _filteredAlerts.Clear();
            if (SelectedTicker == ALL && SelectedTimeframe.ToString() == ALL)
            {
                foreach (var alert in _allAlerts)
                {
                    _filteredAlerts.Add(alert);
                }
            }
            else
            {
                ObservableCollection<Alert> filtered = null;

                if (SelectedTicker == ALL)
                {
                    filtered = new ObservableCollection<Alert>(_allAlerts.Where(x => x.Timeframe.ToString() == SelectedTimeframe));
                }
                else if (SelectedTimeframe.ToString() == ALL)
                {
                    filtered = new ObservableCollection<Alert>(_allAlerts.Where(x => x.Ticker == SelectedTicker));
                }
                else
                {
                    filtered = new ObservableCollection<Alert>(_allAlerts.Where(x => x.Ticker == SelectedTicker && x.Timeframe.ToString() == SelectedTimeframe));
                }

                foreach (var alert in filtered)
                {
                    _filteredAlerts.Add(alert);
                }
            }
            OnPropertyChanged(nameof(Alerts));
        }

        private async Task StartStrategy()
        {
#if DEBUG
            await RunInDebug();
#else
            await RunInRelease();
#endif
        }

        private async Task RunInDebug()
        {
            var tickers = TickersToTrade.POPULAR_TICKERS;
            var timeframes = new[] { Timeframe.Minute15 };
            Connect();

            while (true)
            {
                foreach (var timeframe in timeframes)
                {
                    foreach (var ticker in tickers)
                    {
                        await _repo.FillLatestDataForTheDay(ticker, timeframe, DateTime.Now, DateTime.Now);
                        var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker, timeframe);

                        var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-12), DateTime.Now);

                        for (int i = 6490; i < prices.Count; i++)
                        {
                            //var topsNBottoms = Task.Run(() => _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter));
                            //var downTrendBreakout = Task.Run(() => _strategy.CheckForBreakAboveDownTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter));
                            //var upTrendBreakout = Task.Run(() => _strategy.CheckForBreakBelowUpTrendLine(ticker, prices.Take(i).ToList(), swingPointStrategyParameter));

                            //await Task.WhenAll(topsNBottoms, downTrendBreakout, upTrendBreakout);

                            //await Task.Run(() => _strategy.CheckForTopBottomTouch(ticker, prices.Take(i).ToList(), swingPointStrategyParameter));
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(15.0));
                Logs.Add(new LogEventArg($"Finished running strategy at {DateTime.Now}"));
            }
        }
        private async Task RunInRelease()
        {
            while (true)
            {
                try
                {
                    Logs.Add(new LogEventArg($"Started running strategy at {DateTime.Now}"));

                    var tickers = TickersToTrade.POPULAR_TICKERS;
                    var timeframes = new[] { Timeframe.Minute15, Timeframe.Minute30, Timeframe.Hour1 };

                    foreach (var timeframe in timeframes)
                    {
                        foreach (var ticker in tickers)
                        {
                            await _repo.FillLatestDataForTheDay(ticker, timeframe, DateTime.Now, DateTime.Now);
                            var swingPointStrategyParameter = GetSwingPointStrategyParameter(ticker, timeframe);

                            var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddMonths(-12), DateTime.Now);

                            await Task.Run(() => _strategy.CheckForTopBottomTouch(ticker, prices.ToList(), swingPointStrategyParameter));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logs.Add(new LogEventArg(ex.Message));
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMinutes(15.0));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Strategy_AlertCreated(object sender, AlertEventArgs e)
        {
            lock (_lock)
            {
                var alert = e.Alert;
                if (!_allAlerts.Contains(alert))
                {
                    _allAlerts.Add(alert);
                    CreateTopNBottomBuyOrder(alert).Wait();
                    UpdateFilteredAlerts();
                }
            }
        }

        private async Task CreateTopNBottomBuyOrder(Alert alert)
        {
            if (alert is not TopNBottomStrategyAlert)
            {
                return;
            }

            if (!IsConnected)
            {
                return;
            }

            var swingPointAlert = (TopNBottomStrategyAlert)alert;
            var atr = swingPointAlert.ATR;
            var today = DateTime.Now.Date;
            int numberOfDaysToFriday = (int)DayOfWeek.Friday - (int)today.DayOfWeek;
            var fridayThisWeek = DateTime.Now.AddDays(numberOfDaysToFriday);

            var fridayNextWeek = today.AddDays(numberOfDaysToFriday + 7);
            var availableContracts = await _repo.GetOptionChainAsync(swingPointAlert.Ticker, today, fridayNextWeek);

            if (availableContracts == null || !availableContracts.Active.Any())
            {
                return;
            }

            var optionType = alert.OrderPosition == OrderPosition.Long ? OptionType.C.ToString() : OptionType.P.ToString();
            var strike = alert.OrderPosition == OrderPosition.Long ? swingPointAlert.PriceClosed + atr * 3 : swingPointAlert.PriceClosed - atr * 3;
            Option closestOption = null;
            List<Option> optionsToPick = null;
            if (numberOfDaysToFriday >= 3)
            {
                optionsToPick = availableContracts.ParsedActiveOptions.Where(x => x.ExpiryDate == fridayThisWeek).ToList();
            }
            else
            {
                optionsToPick = availableContracts.ParsedActiveOptions.Where(x => x.ExpiryDate == fridayNextWeek).ToList();
            }

            if (optionsToPick == null || !optionsToPick.Any())
            {
                return;
            }

            var option = optionsToPick.OrderBy(opt => Math.Abs(opt.StrikePrice - strike)).First();

            var order = new Order
            {
                OrderId = 0,
                Action = OrderAction.BUY.ToString(),
                OrderType = OrderType.MKT.ToString(),
                TotalQuantity = 10,
                Transmit = true,
                Account = _accountId,
            };
            var contract = new Contract
            {
                Symbol = swingPointAlert.Ticker,
                SecType = SecType.OPT.ToString(),
                Currency = Currency.USD.ToString(),
                Exchange = "SMART",
                Strike = (double)option.StrikePrice,
                Right = optionType,
                Multiplier = "100",
                LastTradeDateOrContractMonth = option.ExpiryDate.ToString("yyyyMMdd"),
            };

#if DEBUG
            Logs.Add(new LogEventArg($"Placing order for {swingPointAlert.Ticker} {optionType} {option.StrikePrice} {option.ExpiryDate}"));
#else
            _orderManager.PlaceOrder(contract, order);
#endif
        }

        private SwingPointStrategyParameter GetSwingPointStrategyParameter(string ticker, Timeframe timeframe)
        {
            if (timeframe == Timeframe.Daily)
            {
                return new SwingPointStrategyParameter
                {
                    NumberOfCandlesticksToLookBack = 7,
                    Timeframe = timeframe,
                    NumberOfCandlesticksIntersectForTopsAndBottoms = 5,

                    NumberOfSwingPointsToLookBack = 7,
                    NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                    NumberOfTouchesToDrawTrendLine = 2,
                    NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
                    NumberOfCandlesticksBeforeCurrentPriceToLookBack = 7,
                };
            }

            switch (ticker)
            {
                case "AAPL":
                    return new SwingPointStrategyParameter
                    {
                        NumberOfCandlesticksToLookBack = 21,
                        Timeframe = timeframe,
                        NumberOfCandlesticksIntersectForTopsAndBottoms = 5,
                    }.Merge(GetDefaultParameter(timeframe));
                case "QQQ":
                    return new SwingPointStrategyParameter
                    {
                        NumberOfCandlesticksToLookBack = 14,
                        Timeframe = timeframe,
                        NumberOfCandlesticksIntersectForTopsAndBottoms = 5,
                    }.Merge(GetDefaultParameter(timeframe));
                default:
                    return GetDefaultParameter(timeframe);
            }
        }

        private SwingPointStrategyParameter GetDefaultParameter(Timeframe timeframe)
        {
            return new SwingPointStrategyParameter
            {
                NumberOfCandlesticksToLookBack = 21,
                Timeframe = timeframe,
                NumberOfCandlesticksIntersectForTopsAndBottoms = 5,

                NumberOfSwingPointsToLookBack = 7,
                NumberOfCandlesticksToSkipAfterSwingPoint = 2,
                NumberOfTouchesToDrawTrendLine = 2,
                NumberOfCandlesBetweenCurrentPriceAndLastLineEndPoint = 390,
                NumberOfCandlesticksBeforeCurrentPriceToLookBack = 7,
            };
        }
    }
}