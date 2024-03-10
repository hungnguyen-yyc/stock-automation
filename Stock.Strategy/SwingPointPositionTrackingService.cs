using IBApi;
using Stock.Data;
using Stock.Shared.Models;
using Stock.Shared.Models.IBKR.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.Strategies
{
    public class SwingPointPositionTrackingService
    {
        private List<PositionMessage> _positions;
        private StockDataRepository _repo;
        private IDictionary<PositionMessage, IList<decimal>> _positionWithProfitPercentage;

        public event LogEventHander LogCreated;

        public delegate void ClosePositionAlertEventHandler(PositionMessage positionMessage);
        public event ClosePositionAlertEventHandler ClosePositionAlert;

        public SwingPointPositionTrackingService(StockDataRepository repo)
        {
            _positions = new List<PositionMessage>();
            _repo = repo;
            _positionWithProfitPercentage = new Dictionary<PositionMessage, IList<decimal>>();
        }

        public async Task UpdatePosition(PositionMessage positionMessage)
        {
            if (!_positions.Contains(positionMessage))
            {
                _positions.Add(positionMessage);
                Track(Timeframe.Minute15);
            }
        }

        public async Task Track(Timeframe timeframe)
        {
            Log("Start tracking position");
            while (true)
            {
                var openPositions = _positions.Where(p => p.Position != 0).ToList();
                if (openPositions.Count == 0)
                {
                    Log("No position to track");
                    break;
                }

                foreach (var position in openPositions)
                {
                    var ticker = position.Contract.Symbol;
                    await _repo.FillLatestDataForTheDay(ticker, timeframe, DateTime.Now, DateTime.Now);

                    var prices = await _repo.GetStockData(ticker, timeframe, DateTime.Now.AddDays(-1), DateTime.Now);
                    
                    if (position.Contract.Right == "C")
                    {
                        await HandleCallPosition(position, prices);
                    }
                    else
                    {
                        await HandlePutPosition(position, prices);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            Log("Tracking finished");
        }

        private async Task HandlePutPosition(PositionMessage position, IReadOnlyCollection<Price> prices)
        {
            var price = prices.Last();
            var ticker = position.Contract.Symbol;
            var strike = (decimal)position.Contract.Strike;
            var right = position.Contract.Right;
            var avgCost = (decimal)position.AverageCost;
            var expiredOn = DateTime.ParseExact(position.Contract.LastTradeDateOrContractMonth, "yyyyMMdd", null);

            var highLevel = await _repo.GetHigherTargetForContract(position.Contract);

            var optionExpiredDate = DateTime.ParseExact(position.Contract.LastTradeDateOrContractMonth, "yyyyMMdd", null);
            var optionPrice = await _repo.GetOptionPriceAsync(position.Contract.Symbol, optionExpiredDate, (decimal)position.Contract.Strike, position.Contract.Right.ToCharArray()[0]);

            if (optionPrice != null && optionPrice.Any())
            {
                var lastOptionPrice = optionPrice.Last();
                var priceFragments = lastOptionPrice.Split('|');
                var closeFragment = priceFragments.FirstOrDefault(f => f.StartsWith("C:"));
                if (closeFragment != null)
                {
                    var closeContractPrice = decimal.Parse(closeFragment.Trim().Substring(2));
                    var closePriceLessThanCurrentPrice = closeContractPrice + closeContractPrice * (decimal)0.1 < avgCost;
                    if (closePriceLessThanCurrentPrice)
                    {
                        Log($"Price {price.Close} is higher than close price {closeContractPrice} for {ticker} {expiredOn} {strike} {right}");
                        ClosePositionAlert?.Invoke(position);
                    }
                    else
                    {
                        if (_positionWithProfitPercentage.ContainsKey(position))
                        {
                            _positionWithProfitPercentage[position].Add((closeContractPrice - avgCost) / avgCost);
                        }
                        else
                        {
                            _positionWithProfitPercentage.Add(position, new List<decimal> { (closeContractPrice - avgCost) / avgCost });
                        }

                        var profitPercentage = _positionWithProfitPercentage[position];
                        if (profitPercentage.Count >= 4)
                        {
                            var last4ProfitPercentage = profitPercentage.Skip(profitPercentage.Count - 4).ToList();
                            var profitDecreased = last4ProfitPercentage[0] > last4ProfitPercentage[1]
                                && last4ProfitPercentage[1] > last4ProfitPercentage[2]
                                && last4ProfitPercentage[2] > last4ProfitPercentage[3];
                            if (profitDecreased)
                            {
                                Log($"Price {price.Close} is higher than close price {closeContractPrice} for {ticker} {expiredOn} {strike} {right}");
                                ClosePositionAlert?.Invoke(position);
                            }
                        }
                    }
                }
            }
        }

        private async Task HandleCallPosition(PositionMessage position, IReadOnlyCollection<Price> prices)
        {
            var price = prices.Last();
            var ticker = position.Contract.Symbol;
            var strike = (decimal)position.Contract.Strike;
            var right = position.Contract.Right;
            var avgCost = (decimal)position.AverageCost;
            var expiredOn = DateTime.ParseExact(position.Contract.LastTradeDateOrContractMonth, "yyyyMMdd", null);

            var lowLevel = await _repo.GetLowerTargetForContract(position.Contract);


            var optionExpiredDate = DateTime.ParseExact(position.Contract.LastTradeDateOrContractMonth, "yyyyMMdd", null);
            var optionPrice = await _repo.GetOptionPriceAsync(position.Contract.Symbol, optionExpiredDate, (decimal)position.Contract.Strike, position.Contract.Right.ToCharArray()[0]);

            if (optionPrice!= null && optionPrice.Any())
            {
                var lastOptionPrice = optionPrice.Last();
                var priceFragments = lastOptionPrice.Split('|');
                var closeFragment = priceFragments.FirstOrDefault(f => f.StartsWith("C:"));
                if (closeFragment != null)
                {
                    var closeContractPrice = decimal.Parse(closeFragment.Trim().Substring(2));
                    var closePriceLessThanCurrentPrice = closeContractPrice + closeContractPrice * (decimal)0.1 < avgCost;
                    if (closePriceLessThanCurrentPrice)
                    {
                        Log($"Price {price.Close} is higher than close price {closeContractPrice} for {ticker} {expiredOn} {strike} {right}");
                        ClosePositionAlert?.Invoke(position);
                    }
                    else
                    {
                        if (_positionWithProfitPercentage.ContainsKey(position))
                        {
                            _positionWithProfitPercentage[position].Add((closeContractPrice - avgCost) / avgCost);
                        }
                        else
                        {
                            _positionWithProfitPercentage.Add(position, new List<decimal> { (closeContractPrice - avgCost) / avgCost });
                        }

                        var profitPercentage = _positionWithProfitPercentage[position];
                        if (profitPercentage.Count >= 4)
                        {
                            var last4ProfitPercentage = profitPercentage.Skip(profitPercentage.Count - 4).ToList();
                            var profitDecreased = last4ProfitPercentage[0] > last4ProfitPercentage[1]
                                && last4ProfitPercentage[1] > last4ProfitPercentage[2]
                                && last4ProfitPercentage[2] > last4ProfitPercentage[3];
                            if (profitDecreased)
                            {
                                Log($"Price {price.Close} is higher than close price {closeContractPrice} for {ticker} {expiredOn} {strike} {right}");
                                ClosePositionAlert?.Invoke(position);
                            }
                        }
                    }
                }
            }

            if (lowLevel != null && price.Close <= lowLevel)
            {
                Log($"Price {price.Close} is lower than low level {lowLevel} for {ticker} {expiredOn} {strike} {right}");
                ClosePositionAlert?.Invoke(position);
            }
        }

        public void Log(string message)
        {
            LogCreated?.Invoke(new Data.EventArgs.LogEventArg(message));
        }
    }

    public class OpenedPosition : PositionMessage
    {
        public OpenedPosition(string account, Contract contract, decimal pos, double avgCost) : base(account, contract, pos, avgCost)
        {
        }
    }
}
