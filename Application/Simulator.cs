using CStafford.Moneytree.Infrastructure;
using CStafford.Moneytree.Models;
using Microsoft.Extensions.Logging;
using static CStafford.Moneytree.Application.Computer;
using static CStafford.Moneytree.Models.Simulation;

namespace CStafford.Moneytree.Application;

public class Simulator
{
    private readonly Computer _computer;
    private readonly MoneyTreeDbContext _dbContext;
    private readonly ILogger<Simulator> _logger;
    private DateTime _earliestDate;
    private DateTime _latestDate;
    private Random _random;

    public Simulator(
        Computer computer,
        MoneyTreeDbContext dbContext,
        ILogger<Simulator> logger)
    {
        _computer = computer;
        _dbContext = dbContext;
        _logger = logger;
        _random = new Random();
        _earliestDate = _dbContext.Ticks.Min(x => x.OpenTime);
        _latestDate = _dbContext.Ticks.Max(x => x.OpenTime);
    }

    public async Task Run()
    {
        _logger.LogInformation("Setting up charts for simulation runs");

        await EnsureCharts(1000);

        var chartIdToNumSimulations = new Dictionary<int, int>();
        var charts = await _dbContext.Charts.ToListAsync();
        foreach (var chart in charts)
        {
            chartIdToNumSimulations.Add(chart.Id, numS
        }
    }

    private async Task EnsureCharts(int num)
    {
        var currentNum = _dbContext.Charts.Count();

        for (int i = 0; i < num - currentNum; i++)
        {
            var chart = new Chart();
            
            chart.MinutesForMarketAnalysis = _random.Next(30, 2001);
            chart.NumberOfHighestTradedForMarketAnalysis = _random.Next(3, 9);
            chart.DaysSymbolsMustExist = _random.Next(0, 46);
            chart.PercentagePlacementForSecurityPick = (decimal) _random.NextDouble();
            chart.ThresholdToRiseForSell = (0.5m + (decimal) (_random.NextDouble() * 9.5)) / 100m;
            chart.ThresholdToDropForSell = (0.5m + (decimal) (_random.NextDouble() * 9.5)) / 100m;

            await _dbContext.Insert(chart);
        }
    }

    private async Task RunSimulation(int chartId)
    {

            var simulation = new Simulation();

            simulation.DepositFrequency = (DepositFrequencyEnum)_random.Next(0, 3);
            
            var lowerRange = _earliestDate.Add(TimeSpan.FromDays(chart.DaysSymbolsMustExist));
            var availableMinutes = _latestDate.Subtract(lowerRange).TotalMinutes;

            simulation.SimulationStart = lowerRange.AddMinutes(_random.Next(0, (int)availableMinutes));
            availableMinutes = _latestDate.Subtract(simulation.SimulationStart).TotalMinutes;
            simulation.SimulationEnd = simulation.SimulationStart.AddMinutes(_random.Next(0, (int)availableMinutes));
            simulation.RunTimeStart = DateTime.Now;
            simulation.ChartId = chart.Id;
            
            var cashDeposited = 0m;
            var cashOnHand = 0m;
            var assets = new List<(string symbol, decimal usdPurchasePrice, decimal quanityOwned)>();

            var current = simulation.SimulationStart;
            var nextDeposit = current;

            while (current <= simulation.SimulationEnd)
            {
                if (current == nextDeposit)
                {
                    cashDeposited += 100m;
                    cashOnHand += 100m;

                    switch(simulation.DepositFrequency)
                    {
                        case DepositFrequencyEnum.Monthly:
                            nextDeposit = nextDeposit.AddMonths(1);
                            break;
                        case DepositFrequencyEnum.Weekly:
                            nextDeposit = nextDeposit.AddDays(7);
                            break;
                        case DepositFrequencyEnum.Daily:
                            nextDeposit = nextDeposit.AddDays(1);
                            break;
                    }
                }
                
                var actionsToTake = await _computer.EvaluateMarket(
                    chart,
                    cashOnHand > 0m,
                    assets.Select(x => (x.symbol, x.usdPurchasePrice)).ToList(),
                    current);
                
                foreach (var actionToTake in actionsToTake)
                {
                    switch (actionToTake.action)
                    {
                        case ActionToTake.Buy:
                            if (! assets.Any(x => x.symbol == actionToTake.relevantSymbol))
                            {
                                var qtyToBuy = cashOnHand / actionToTake.symbolUsdValue.Value;
                                assets.Add((actionToTake.relevantSymbol, actionToTake.symbolUsdValue.Value, qtyToBuy));
                            }
                            else
                            {
                                var existing = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                                assets.Remove(existing);

                                // calculate a viable usdPurchasePrice porportional to what's owned and what's being bought now
                                var qtyToBuy = cashOnHand / actionToTake.symbolUsdValue.Value;
                                var totalQty = qtyToBuy + existing.quanityOwned;
                                var portionOriginalPrice = existing.quanityOwned / totalQty;
                                var portionNewPrice = qtyToBuy / totalQty;
                                var mediatedBuyPrice = (portionOriginalPrice * existing.quanityOwned) + (portionNewPrice * qtyToBuy);

                                assets.Add((actionToTake.relevantSymbol, mediatedBuyPrice, totalQty));
                            }

                            cashOnHand = 0m;
                            break;
                        case ActionToTake.Sell:
                            var asset = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                            cashOnHand += actionToTake.symbolUsdValue.Value * asset.quanityOwned;
                            assets.Remove(asset);
                            break;
                        case ActionToTake.Hold:
                            break;
                    }
                }

                current = current.AddMinutes(1);
            }

            simulation.RunTimeEnd = DateTime.Now;
            foreach (var asset in assets)
            {
                cashOnHand += asset.quanityOwned + await _computer.MarketValue(asset.symbol, current);
            }
            var gain = (cashOnHand - cashDeposited) / cashDeposited;
            simulation.ResultGainPercentage = gain;
            await _dbContext.Insert(simulation);

            _logger.LogInformation($"---------------\nRun {i + 1} of 1000");
            _logger.LogInformation("\n---------------CHART:\n---------------");
            _logger.LogInformation(chart.ToString());
            _logger.LogInformation("\n---------------Simulation:\n---------------");
            _logger.LogInformation(simulation.ToString());
        }
    }
}
