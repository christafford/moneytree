using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using static CStafford.MoneyTree.Application.Computer;
using static CStafford.MoneyTree.Models.Simulation;

namespace CStafford.MoneyTree.Application;

public class Simulator
{
    private readonly Computer _computer;
    private readonly MoneyTreeDbContext _dbContext;
    private readonly DbContextOptions<MoneyTreeDbContext> _options;
    private readonly ILogger<Simulator> _logger;
    private DateTime _earliestDate;
    private DateTime _latestDate;
    private List<(Chart chart, Simulation simulation, ComputerContext context)> _simulationsToRun;
    private Random _random;

    public Simulator(
        DbContextOptions<MoneyTreeDbContext> options,
        Computer computer,
        ILogger<Simulator> logger)
    {
        _computer = computer;
        _dbContext = new MoneyTreeDbContext(options);
        _options = options;
        _logger = logger;
        _random = new Random(System.DateTime.Now.Millisecond);
        _earliestDate = _dbContext.Ticks.Min(x => x.OpenTime);
        _latestDate = _dbContext.Ticks.Max(x => x.OpenTime);
        _simulationsToRun = new List<(Chart chart, Simulation simulation, ComputerContext context)>();
    }

    public async Task Run()
    {
        _logger.LogInformation("Setting up charts for simulation runs");

        await EnsureCharts(1000);

        var chartIdToNumSimulations = new Dictionary<Chart, int>();
        var charts = await _dbContext.Charts.ToListAsync();

        foreach (var chart in charts)
        {
            chartIdToNumSimulations.Add(chart, await _dbContext.Simulations.CountAsync(x => x.ChartId == chart.Id));
        }

        _logger.LogInformation("Done. Now running simulations");

        for (int i = 0; i < 3; i++)
        {
            var lowestChart = chartIdToNumSimulations.OrderBy(x => x.Value).First().Key;
            chartIdToNumSimulations[lowestChart]++;
            await AddSimulation(lowestChart);
            _logger.LogInformation("Added simulation {0}", i);
        }

        var simulationTasks = new List<Task>();
        foreach (var simulation in _simulationsToRun)
        {
            simulationTasks.Add(RunSimulation(simulation.chart, simulation.simulation, simulation.context));
        }

        Task.WaitAll(simulationTasks.ToArray());
        _logger.LogInformation("Done running simulations");
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

    private async Task AddSimulation(Chart chart)
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

        var computerContext = new ComputerContext(_options);
        await computerContext.Init(_dbContext, chart, simulation.SimulationStart);

        _simulationsToRun.Add((chart, simulation, computerContext));
    }

    private async Task RunSimulation(Chart chart, Simulation simulation, ComputerContext computerContext)
    {
        _logger.LogInformation("Running simulation for {chart} and simulation {simulation}", chart, simulation);

        var cashDeposited = 0m;
        var cashOnHand = 0m;
        var assets = new List<(string symbol, decimal usdPurchasePrice, decimal quantityOwned)>();

        var nextDeposit = computerContext.EvaluationTime;

        while (computerContext.EvaluationTime <= simulation.SimulationEnd)
        {
            if (computerContext.EvaluationTime == nextDeposit)
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
            
            var actionsToTake = _computer.EvaluateMarket(
                chart,
                cashOnHand > 0m,
                assets.Select(x => (x.symbol, x.usdPurchasePrice)).ToList(),
                computerContext);
            
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

                            // calculate a viable usdPurchasePrice proportional to what's owned and what's being bought now
                            var qtyToBuy = cashOnHand / actionToTake.symbolUsdValue.Value;
                            var totalQty = qtyToBuy + existing.quantityOwned;
                            var portionOriginalPrice = existing.quantityOwned / totalQty;
                            var portionNewPrice = qtyToBuy / totalQty;
                            var mediatedBuyPrice = (portionOriginalPrice * existing.quantityOwned) + (portionNewPrice * qtyToBuy);

                            assets.Add((actionToTake.relevantSymbol, mediatedBuyPrice, totalQty));
                        }

                        cashOnHand = 0m;
                        break;
                    case ActionToTake.Sell:
                        var asset = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                        cashOnHand += actionToTake.symbolUsdValue.Value * asset.quantityOwned;
                        assets.Remove(asset);
                        break;
                    case ActionToTake.Hold:
                        break;
                }
            }

            await computerContext.NextTick();
        }

        simulation.RunTimeEnd = DateTime.Now;

        foreach (var asset in assets)
        {
            cashOnHand += asset.quantityOwned + (await _computer.MarketValue(asset.symbol, computerContext.EvaluationTime));
        }

        var gain = (cashOnHand - cashDeposited) / cashDeposited;
        simulation.ResultGainPercentage = gain;

        await _dbContext.Insert(simulation);

        _logger.LogInformation("\n---------------Simulation:\n---------------");
        _logger.LogInformation(simulation.ToString());
    }
}