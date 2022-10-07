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
        var chart = new Chart();
        
        chart.MinutesForMarketAnalysis = _random.Next(30, 2001);
        chart.NumberOfHighestTradedForMarketAnalysis = _random.Next(3, 9);
        chart.DaysSymbolsMustExist = _random.Next(0, 46);
        chart.PercentagePlacementForSecurityPick = (decimal) _random.NextDouble();
        chart.ThresholdToRiseForSell = (0.5m + (decimal) (_random.NextDouble() * 9.5)) / 100m;
        chart.ThresholdToDropForSell = (0.5m + (decimal) (_random.NextDouble() * 9.5)) / 100m;

        await _dbContext.Insert(chart);

        _logger.LogInformation("Created new {chart}", chart);

        var simulations = new List<Simulation>();

        for (int i = 0; i < 1000; i++)
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
            var assets = new List<(string symbol, decimal usdInvested, decimal quantityOwned)>();
            
            var current = simulation.RunTimeStart;
            var nextDeposit = current;

            while (current <= simulation.RunTimeEnd)
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
                    assets,
                    current);
                
                foreach (var actionToTake in actionsToTake)
                {
                    switch (actionToTake.action)
                    {
                        case ActionToTake.Buy:
                            if (! assets.Any(x => x.symbol == actionToTake.relevantSymbol))
                            {
                                assets.Add((actionToTake.relevantSymbol, cashOnHand));
                            }
                            else
                            {
                                var existing = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                                assets.Remove(existing);
                                assets.Add((actionToTake.relevantSymbol, existing.usdAtPurchase + cashOnHand));
                            }
                            cashOnHand = 0m;
                            break;
                        case ActionToTake.Sell:
                            var asset = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                            cashOnHand += actionToTake.symbolUsdValue * asset.usdAtPurchase;
                            assets.Remove(asset);
                            break;
                        case ActionToTake.Hold:
                            break;
                    }
                }

                current = current.AddMinutes(1);
            }
        }
    }
}
