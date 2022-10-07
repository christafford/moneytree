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

        public async Task<decimal> MarketValue(string symbol, DateTime atDate)
        {
            var symbolId = (await _dbContext.Symbols.FirstAsync(x => x.Name == symbol)).Id;

            var tick = await _dbContext.Ticks
                .Where(x => x.SymbolId == symbolId)
                .Where(x => x.OpenTime <= atDate)
                .Where(x => x.ClosePrice != null)
                .OrderByDescending(x => x.OpenTime)
                .FirstAsync();

            return tick.ClosePrice.Value;
        }

        public async Task<List<(ActionToTake action, string relevantSymbol, decimal? symbolUsdValue)>> EvaluateMarket(
            Chart chart,
            bool moneyToBurn,
            List<(string symbol, decimal usdInvested)> assets,
            DateTime evaluationTime)
        {
            var toReturn = new List<(ActionToTake action, string relevantSymbol, decimal? symbolUsdValue)>();
            var symbolIdToName = _dbContext.Symbols.ToDictionary(x => x.Id, x => x.Name);
            
            if (moneyToBurn)
            {
                var ticks = await _dbContext.Ticks
                    .Where(x => x.OpenTime >= evaluationTime.Subtract(TimeSpan.FromMinutes(chart.MinutesForMarketAnalysis)))
                    .Where(x => x.OpenTime <= evaluationTime)
                    .ToListAsync();

                var symbols = ticks.Select(x => x.SymbolId).Distinct();
                var validationDate = evaluationTime.Subtract(TimeSpan.FromDays(chart.DaysSymbolsMustExist));

                var passesExistenceValidation = symbols
                    .Where(x => _dbContext.Ticks
                        .Where(y => y.SymbolId == x)
                        .Where(y => y.OpenTime <= validationDate)
                        .Any())
                    .ToHashSet();

                ticks = ticks
                    .Where(x => passesExistenceValidation.Contains(x.SymbolId))
                    .OrderBy(x => x.OpenTime)
                    .ToList();

                symbols = ticks.Select(x => x.SymbolId).Distinct();
                
                var marketMovers = ticks
                    .Select(x => x.SymbolId)
                    .Distinct()
                    .OrderByDescending(x => ticks
                        .Where(y => y.SymbolId == x)
                        .Sum(y => y.ClosePrice * y.Volume))
                    .Take(chart.NumberOfHighestTradedForMarketAnalysis);

                if (marketMovers.All(x =>
                {
                    var first = ticks.Where(y => y.SymbolId == x).First();
                    var last = ticks.Where(y => y.SymbolId == x).Last();

                    return first.OpenPrice < last.ClosePrice;
                }))
                {
                    var sortedSymbols = symbols
                        .OrderByDescending(x =>
                        {
                            var first = (ticks.First(y => y.SymbolId == x)).OpenPrice;
                            var last = (ticks.Last(y => y.SymbolId == x)).ClosePrice;

                            return (last - first) / first;
                        })
                        .ToList();

                    int symbolIdToBuy = default;

                    // i'm being stupid at this point, fix later
                    for (int i = 0; i < sortedSymbols.Count(); i++)
                    {
                        if ((i / (decimal) sortedSymbols.Count()) < chart.PercentagePlacementForSecurityPick)
                        {
                            symbolIdToBuy = sortedSymbols[i];
                        }
                        else
                        {
                            break;
                        }
                    }

                    toReturn.Add((
                        ActionToTake.Buy,
                        symbolIdToName[symbolIdToBuy],
                        ticks.Where(x => x.SymbolId == symbolIdToBuy).Last().ClosePrice));
                }
            }

            foreach (var asset in assets)
            {
                var tick = _dbContext.Ticks
                    .Where(x => symbolIdToName[x.SymbolId] == asset.symbol)
                    .Where(x => x.OpenTime <= evaluationTime)
                    .OrderByDescending(x => x.OpenTime)
                    .First();
                
                var diff = (tick.ClosePrice - asset.usdInvested) / asset.usdAtPurchase;
                
                if (diff >= chart.ThresholdToRiseForSell || diff <= chart.ThresholdToDropForSell)
                {
                    toReturn.Add((ActionToTake.Sell, asset.symbol, tick.ClosePrice));
                }
            }

            return toReturn;
        }
    }
}