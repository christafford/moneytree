using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CStafford.MoneyTree.Application
{
    public class Computer
    {
        public enum ActionToTake
        {
            Buy,
            Sell,
            Hold
        }

        private readonly ILogger<Computer> _logger;
        private Dictionary<int, string> _symbolIdToName;
        private Dictionary<string, int> _symbolNameToId;

        public Computer(MoneyTreeDbContext dbContext, ILogger<Computer> logger)
        {
            _logger = logger;

            _symbolIdToName = dbContext.Symbols.ToDictionary(x => x.Id, x => x.Name);
            _symbolNameToId = dbContext.Symbols.ToDictionary(x => x.Name, x => x.Id);
        }

        public decimal MarketValue(string symbol, int atEpoch, MoneyTreeDbContext dbContext)
        {
            var symbolId = _symbolNameToId[symbol];
            return dbContext.MarketValue(symbolId, atEpoch);
        }

        public List<(ActionToTake action, string relevantSymbol, decimal? symbolUsdValue)> EvaluateMarket(
            Chart chart,
            bool moneyToBurn,
            List<(string symbol, decimal avgPricePaid)> assets,
            ComputerContext computerContext)
        {
            var toReturn = new List<(ActionToTake action, string relevantSymbol, decimal? symbolUsdValue)>();
            var marketContext = computerContext.MarketAnalysis();

            if (marketContext.Any())
            {
                if (moneyToBurn)
                {
                    var marketMovers = marketContext
                        .OrderByDescending(x => x.volumeUsd)
                        .Take(chart.NumberOfHighestTradedForMarketAnalysis);

                    if (marketMovers.All(x => x.percentageGain > 0))
                    {
                        var marketGainers = marketContext
                            .OrderByDescending(x => x.percentageGain)
                            .ToList();

                        (int symbolId, decimal volumeUsd, decimal percentageGain, decimal closePrice) toBuy = default;

                        // i'm being stupid at this point, fix later
                        for (int i = 0; i < marketGainers.Count(); i++)
                        {
                            if ((i / (decimal) marketGainers.Count()) < chart.PercentagePlacementForSecurityPick)
                            {
                                toBuy = marketGainers[i];
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (toBuy == default)
                        {
                            Console.WriteLine("Nonsense - how can there be no reasonable value for what we need here.");
                        }

                        toReturn.Add((
                            ActionToTake.Buy,
                            _symbolIdToName[toBuy.symbolId],
                            toBuy.closePrice));
                    }
                }

                foreach (var asset in assets)
                {
                    var assetSymbolId = _symbolNameToId[asset.symbol];
                    var assetCurrent = marketContext.FirstOrDefault(x => x.symbolId == assetSymbolId);
                    
                    if (assetCurrent == default)
                    {
                        continue;
                    }

                    var diff = (assetCurrent.closePrice - asset.avgPricePaid) / asset.avgPricePaid;
                    
                    if (diff >= chart.ThresholdToRiseForSell || (diff * -1) >= chart.ThresholdToDropForSell)
                    {
                        toReturn.Add((ActionToTake.Sell, asset.symbol, assetCurrent.closePrice));
                    }
                }
            }

            return toReturn;
        }
    }
}