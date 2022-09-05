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

        public async Task<List<(ActionToTake action, string relevantSymbol)>> EvaluateMarket(
            Chart chart,
            bool moneyToBurn,
            List<(string symbol, decimal usdAtPurchase)> assets,
            DateTime evaluationTime)
        {
            var toReturn = new List<(ActionToTake action, string relevantSymbol)>();

            if (moneyToBurn)
            {
                var ticks = await _dbContext.Ticks
                    .Where(x => x.OpenTime >= evaluationTime.Subtract(TimeSpan.FromMinutes(chart.MinutesForMarketAnalysis)))
                    .Where(x => x.OpenTime <= evaluationTime)
                    .OrderBy(x => x.OpenTime)
                    .ToListAsync();

                var symbols = ticks.Select(x => x.SymbolName).Distinct();
                var validationDate = evaluationTime.Subtract(TimeSpan.FromDays(chart.DaysSymbolsMustExist));

                var passesExistenceValidation = symbols
                    .Where(x => _dbContext.Ticks
                        .Where(y => y.SymbolName == x)
                        .Where(y => y.OpenTime <= validationDate)
                        .Any())
                    .ToHashSet();

                var filteredMarketTicks = ticks
                    .Where(x => passesExistenceValidation.Contains(x.SymbolName))
                    .ToList();
                
                var marketMovers = filteredMarketTicks
                    .Select(x => x.SymbolName)
                    .Distinct()
                    .OrderByDescending(x => filteredMarketTicks
                        .Where(y => y.SymbolName == x)
                        .Sum(y => y.ClosePrice * y.Volume))
                    .Take(chart.NumberOfHighestTradedForMarketAnalysis);

                if (marketMovers.All(x =>
                {
                    var first = filteredMarketTicks.Where(y => y.SymbolName == x).First();
                    var last = filteredMarketTicks.Where(y => y.SymbolName == x).Last();

                    return first.OpenPrice < last.ClosePrice;
                }))
                {
                    var sortedSymbols = symbols.OrderBy(x =>
                    {
                        var first = (ticks.First(y => y.SymbolName == x)).OpenPrice;
                        var last = (ticks.Last(y => y.SymbolName == x)).ClosePrice;

                        return (last - first) / first;
                    });
                }
            }

            return toReturn;
        }
    }
}