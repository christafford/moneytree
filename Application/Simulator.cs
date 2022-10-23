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

    private static DateTime _lastReported = DateTime.Now;
    private static int _numTicksSinceReport = 0;
    private static Object _lock = new Object();

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

        for (int i = 0; i < 10; i++)
        {
            var lowestChart = chartIdToNumSimulations.OrderBy(x => x.Value).First().Key;
            chartIdToNumSimulations[lowestChart]++;
            try
            {
                await AddSimulation(lowestChart);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Sequence contains no matching element"))
                {
                    Console.WriteLine("Error - symbol wasn't downloaded over span");
                    continue;
                }
                throw;
            }

            _logger.LogInformation("Added simulation {0}", i);
        }

        var simulationTasks = new List<Task>();
        foreach (var simulation in _simulationsToRun)
        {
            simulationTasks.Add(RunSimulation(simulation.chart, simulation.simulation, simulation.context));
        }
        Task.WaitAll(simulationTasks.ToArray());
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

        var dbOptionsBuilder = new DbContextOptionsBuilder<MoneyTreeDbContext>()
            .UseMySql(Constants.ConnectionString,
                ServerVersion.AutoDetect(Constants.ConnectionString),
                mySqlOptions =>
                {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(1),
                        errorNumbersToAdd: null);
                    mySqlOptions.EnableStringComparisonTranslations(true);

                });
                
        var newContext = new MoneyTreeDbContext(dbOptionsBuilder.Options);

        var computerContext = new ComputerContext(newContext);
        await computerContext.Init(_dbContext, chart, simulation.StartEpoch);

        _simulationsToRun.Add((chart, simulation, computerContext));
    }

    private async Task RunSimulation(Chart chart, Simulation simulation, ComputerContext computerContext)
    {
        _logger.LogInformation("Running simulation for:\n{chart}\nand simulation:\n{simulation}", chart, simulation);

        var cashDeposited = 0m;
        var cashOnHand = 0m;
        var assets = new List<(string symbol, decimal usdPurchasePrice, decimal quantityOwned)>();

        var nextDeposit = computerContext.EvaluationEpoch;

        Console.WriteLine("Starting");

        while (computerContext.EvaluationEpoch <= simulation.EndEpoch)
        {
            _numTicksSinceReport++;

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

            _ = await computerContext.NextTick();

            var elapsedReport = DateTime.Now.Subtract(_lastReported).TotalSeconds;

            if (elapsedReport > 30)
            {
                lock(_lock)
                {
                    if (DateTime.Now.Subtract(_lastReported).TotalSeconds > 30)
                    {
                        Console.WriteLine($"Did {_numTicksSinceReport} ticks in {elapsedReport} seconds, " +
                            $"{(_numTicksSinceReport / elapsedReport)} ticks per second");
                        
                        _numTicksSinceReport = 0;
                        _lastReported = DateTime.Now;
                    }       
                }
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

        _logger.LogInformation("\n---------------\nSimulation:\n---------------");
        _logger.LogInformation(simulation.ToString());
    }
}