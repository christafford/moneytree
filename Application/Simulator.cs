using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using static CStafford.MoneyTree.Application.Computer;
using static CStafford.MoneyTree.Models.Simulation;
using CStafford.MoneyTree.Configuration;

namespace CStafford.MoneyTree.Application;

public class Simulator
{
    private readonly Computer _computer;
    private readonly MoneyTreeDbContext _dbContext;
    private readonly ILogger<Simulator> _logger;
    private int _latestFromStart;
    private List<(Chart chart, Simulation simulation, ComputerContext context)> _simulationsToRun;
    private Random _random;

    public Simulator(
        DbContextOptions<MoneyTreeDbContext> options,
        Computer computer,
        ILogger<Simulator> logger)
    {
        _computer = computer;
        _dbContext = new MoneyTreeDbContext(options);
        _logger = logger;
        _random = new Random(System.DateTime.Now.Millisecond);
        _latestFromStart = _dbContext.Ticks.Max(x => x.TickEpoch);
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

        for (int i = 0; i < 1; i++)
        {
            var lowestChart = chartIdToNumSimulations.OrderBy(x => x.Value).First().Key;
            chartIdToNumSimulations[lowestChart]++;
            await AddSimulation(lowestChart);
            _logger.LogInformation("Added simulation {0}", i);
        }

        var simulationTasks = new List<Task>();
        foreach (var simulation in _simulationsToRun)
        {
            await RunSimulation(simulation.chart, simulation.simulation, simulation.context);
        }

        // Task.WaitAll(simulationTasks.ToArray());
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
        
        var lowerRange = chart.DaysSymbolsMustExist * 24 * 60;
        var availableMinutes = _latestFromStart - lowerRange;

        simulation.StartEpoch = lowerRange + _random.Next(0, availableMinutes);
        availableMinutes = _latestFromStart - simulation.StartEpoch;
        simulation.EndEpoch = simulation.StartEpoch + _random.Next(0, availableMinutes);
        simulation.RunTimeStart = DateTime.Now;
        simulation.ChartId = chart.Id;

        var computerContext = new ComputerContext(_dbContext);
        await computerContext.Init(_dbContext, chart, simulation.StartEpoch);

        _simulationsToRun.Add((chart, simulation, computerContext));
    }

    private async Task RunSimulation(Chart chart, Simulation simulation, ComputerContext computerContext)
    {
        _logger.LogInformation("Running simulation for {chart} and simulation {simulation}", chart, simulation);

        var cashDeposited = 0m;
        var cashOnHand = 0m;
        var assets = new List<(string symbol, decimal usdPurchasePrice, decimal quantityOwned)>();

        var nextDeposit = computerContext.EvaluationEpoch;

        var lastReport = DateTime.Now;
        var ticksForReport = 0;
        var evaluateMarketMs = 0d;
        var takeActionsMs = 0d;
        var nextTickMs = 0d;
        var nextTickDbTimeMs = 0d;
        var totalTicksDone = 0;
        var totalTicks = simulation.EndEpoch - simulation.StartEpoch;

        Console.WriteLine("Starting");

        while (computerContext.EvaluationEpoch <= simulation.EndEpoch)
        {
            ticksForReport++;
            totalTicksDone++;

            if (computerContext.EvaluationEpoch == nextDeposit)
            {
                cashDeposited += 100m;
                cashOnHand += 100m;

                switch(simulation.DepositFrequency)
                {
                    case DepositFrequencyEnum.Monthly:
                        nextDeposit = nextDeposit + (30 * 24 * 60);
                        break;
                    case DepositFrequencyEnum.Weekly:
                        nextDeposit = nextDeposit + (24 * 60);
                        break;
                    case DepositFrequencyEnum.Daily:
                        nextDeposit = nextDeposit + 24;
                        break;
                }
            }
            
            var evaluateMarketStart = DateTime.Now;

            var actionsToTake = _computer.EvaluateMarket(
                chart,
                cashOnHand > 0m,
                assets.Select(x => (x.symbol, x.usdPurchasePrice)).ToList(),
                computerContext);
            
            evaluateMarketMs += DateTime.Now.Subtract(evaluateMarketStart).TotalMilliseconds;

            var takeActionsStart = DateTime.Now;

            foreach (var actionToTake in actionsToTake)
            {
                switch (actionToTake.action)
                {
                    case ActionToTake.Buy:
                        if (! assets.Any(x => x.symbol == actionToTake.relevantSymbol))
                        {
                            if (actionToTake.symbolUsdValue.Value == 0)
                            {
                                Console.WriteLine("We have a divide by zero situation");
                                Console.WriteLine($"actionToTake.relevantSymbol: {actionToTake.relevantSymbol}");
                                Console.WriteLine($"Evaluation time: {(Constants.Epoch.AddMinutes(computerContext.EvaluationEpoch)).ToString("g")}");
                                Console.WriteLine($"Cash on hand: {cashOnHand.ToString("C")}");

                                throw new Exception();
                            }

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

            takeActionsMs += DateTime.Now.Subtract(takeActionsStart).TotalMilliseconds;

            var nextTickStart = DateTime.Now;

            var results = await computerContext.NextTick();
            nextTickDbTimeMs += results.dbQueryMs;

            nextTickMs += DateTime.Now.Subtract(nextTickStart).TotalMilliseconds;

            var elapsedMs = (DateTime.Now - lastReport).TotalMilliseconds;
            
            if (elapsedMs > 30 * 1000)
            {
                var elapsedEvaluateMarketMs = evaluateMarketMs / ticksForReport;
                var elapsedTakeActionsMs = takeActionsMs / ticksForReport;
                var elapsedNextTickMs = nextTickMs / ticksForReport;

                Console.WriteLine($"Elapsed: {elapsedMs}ms, EvaluateMarket: {elapsedEvaluateMarketMs}ms, TakeActions: {elapsedTakeActionsMs}ms, NextTick: {elapsedNextTickMs}ms");
                Console.WriteLine($"NextTickDbTime: {nextTickDbTimeMs / ticksForReport}ms");
                Console.WriteLine($"Did {ticksForReport} ticks in {elapsedMs}ms, {ticksForReport / (elapsedMs / 1000)} ticks per second");
                Console.WriteLine($"{totalTicksDone / (double)totalTicks * 100}% complete");

                evaluateMarketMs = 0d;
                takeActionsMs = 0d;
                nextTickMs = 0d;
                ticksForReport = 0;
                nextTickDbTimeMs = 0;
                lastReport = DateTime.Now;                
            }
        }

        simulation.RunTimeEnd = DateTime.Now;

        foreach (var asset in assets)
        {
            cashOnHand += asset.quantityOwned + (await _computer.MarketValue(asset.symbol, computerContext.EvaluationEpoch));
        }

        var gain = (cashOnHand - cashDeposited) / cashDeposited;
        simulation.ResultGainPercentage = gain;

        await _dbContext.Insert(simulation);

        _logger.LogInformation("\n---------------Simulation:\n---------------");
        _logger.LogInformation(simulation.ToString());
    }
}