using CStafford.Moneytree.Infrastructure;
using CStafford.Moneytree.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CStafford.Moneytree.Application
{
    public class Computer
    {
        public enum ActionToTake
        {
            Buy,
            Sell,
            Hold
        }

        private readonly MoneyTreeDbContext _dbContext;
        private readonly ILogger<Computer> _logger;

        public Computer(MoneyTreeDbContext dbContext, ILogger<Computer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<(ActionToTake action, string relevantSymbol, decimal? usdAtPurchase)>> EvaluateMarket(
            Chart chart,
            bool moneyToBurn,
            List<(string symbol, decimal usdAtPurchase)> assets,
            DateTime evaluationTime)
        {
            var toReturn = new List<(ActionToTake action, string relevantSymbol, decimal? usdAtPurchase)>();

            if (moneyToBurn)
            {
                var ticks = await _dbContext.Ticks
                    .Where(x => x.OpenTime >= evaluationTime.Subtract(TimeSpan.FromMinutes(chart.MinutesForMarketAnalysis)))
                    .Where(x => x.OpenTime <= evaluationTime)
                    .ToListAsync();

                var symbols = ticks.Select(x => x.SymbolName).Distinct();
                var validationDate = evaluationTime.Subtract(TimeSpan.FromDays(chart.DaysSymbolsMustExist));

                var passesExistenceValidation = symbols
                    .Where(x => _dbContext.Ticks
                        .Where(y => y.SymbolName == x)
                        .Where(y => y.OpenTime <= validationDate)
                        .Any())
                    .ToHashSet();

                ticks = ticks
                    .Where(x => passesExistenceValidation.Contains(x.SymbolName))
                    .OrderBy(x => x.OpenTime)
                    .ToList();

                symbols = ticks.Select(x => x.SymbolName).Distinct();
                
                var marketMovers = ticks
                    .Select(x => x.SymbolName)
                    .Distinct()
                    .OrderByDescending(x => ticks
                        .Where(y => y.SymbolName == x)
                        .Sum(y => y.ClosePrice * y.Volume))
                    .Take(chart.NumberOfHighestTradedForMarketAnalysis);

                if (marketMovers.All(x =>
                {
                    var first = ticks.Where(y => y.SymbolName == x).First();
                    var last = ticks.Where(y => y.SymbolName == x).Last();

                    return first.OpenPrice < last.ClosePrice;
                }))
                {
                    var sortedSymbols = symbols
                        .OrderByDescending(x =>
                        {
                            var first = (ticks.First(y => y.SymbolName == x)).OpenPrice;
                            var last = (ticks.Last(y => y.SymbolName == x)).ClosePrice;

                            return (last - first) / first;
                        })
                        .ToList();

                    string symbolToBuy = null;

                    // i'm being stupid at this point, fix later
                    for (int i = 0; i < sortedSymbols.Count(); i++)
                    {
                        if ((i / (decimal) sortedSymbols.Count()) < chart.PercentagePlacementForSecurityPick)
                        {
                            symbolToBuy = sortedSymbols[i];
                        }
                        else
                        {
                            break;
                        }
                    }

                    toReturn.Add((
                        ActionToTake.Buy,
                        symbolToBuy,
                        ticks.Where(x => x.SymbolName == symbolToBuy).Last().ClosePrice));
                }
            }

            foreach (var asset in assets)
            {
                var tick = _dbContext.Ticks
                    .Where(x => x.SymbolName == asset.symbol)
                    .Where(x => x.OpenTime <= evaluationTime)
                    .OrderByDescending(x => x.OpenTime)
                    .First();
                
                var diff = (tick.ClosePrice - asset.usdAtPurchase) / asset.usdAtPurchase;
                
                if (diff >= chart.ThresholdToRiseForSell || diff <= chart.ThresholdToDropForSell)
                {
                    toReturn.Add((ActionToTake.Sell, asset.symbol, default));
                }
            }

            return toReturn;
        }
    }
}