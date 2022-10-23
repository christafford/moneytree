using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using static CStafford.MoneyTree.Application.Computer;
using static CStafford.MoneyTree.Models.Simulation;
using CStafford.MoneyTree.Configuration;
using System.Collections.Concurrent;

namespace CStafford.MoneyTree.Application;

public class Simulator
{
    private readonly Computer _computer;
    private readonly MoneyTreeDbContext _dbContext;
    private readonly ILogger<Simulator> _logger;
    private int _latestFromStart;
    private ConcurrentQueue<Chart> _charts;
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
        _latestFromStart = _dbContext.Ticks.Max(x => x.TickEpoch);
        _charts = new ConcurrentQueue<Chart>();
    }

    public async Task Run()
    {
        Console.WriteLine("Setting up charts for simulation runs");

        await EnsureCharts(1000);
        foreach (var chart in _dbContext.Charts)
        {
            _charts.Enqueue(chart);
        }

        var chartIdToNumSimulations = new Dictionary<Chart, int>();
        var charts = await _dbContext.Charts.ToListAsync();

        foreach (var chart in charts)
        {
            chartIdToNumSimulations.Add(chart, await _dbContext.Simulations.CountAsync(x => x.ChartId == chart.Id));
        }

        Console.WriteLine("Done. Now running simulations");

        var threads = new List<Thread>();
        for (int i = 0; i < 15; i++)
        {
            var thread = new Thread(new ThreadStart(Worker));
            thread.Start();
            threads.Add(thread);
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }   
    }

    private async Task EnsureCharts(int num)
    {
        var currentNum = _dbContext.Charts.Count();

        var random = new Random();

        for (int i = 0; i < num - currentNum; i++)
        {
            var chart = new Chart();
            
            chart.MinutesForMarketAnalysis = random.Next(30, 2001);
            chart.NumberOfHighestTradedForMarketAnalysis = random.Next(3, 9);
            chart.DaysSymbolsMustExist = random.Next(0, 46);
            chart.PercentagePlacementForSecurityPick = (decimal) random.NextDouble();
            chart.ThresholdToRiseForSell = (0.5m + (decimal) (random.NextDouble() * 9.5)) / 100m;
            chart.ThresholdToDropForSell = (0.5m + (decimal) (random.NextDouble() * 9.5)) / 100m;

            await _dbContext.Insert(chart);
        }
    }

    private void Worker()
    {
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
                
        var workerDbContext = new MoneyTreeDbContext(dbOptionsBuilder.Options);
        var computerContext = new ComputerContext(workerDbContext);
        var random = new Random();

        while (true)
        {
            if (!_charts.TryDequeue(out Chart chart))
            {
                Thread.Sleep(1000);
                continue;
            }

            _charts.Enqueue(chart);

            var simulation = new Simulation();

            simulation.DepositFrequency = (DepositFrequencyEnum)random.Next(0, 3);
            
            var lowerRange = chart.DaysSymbolsMustExist * 24 * 60;
            var availableMinutes = _latestFromStart - lowerRange;

            simulation.StartEpoch = lowerRange + random.Next(0, availableMinutes);
            availableMinutes = _latestFromStart - simulation.StartEpoch;
            simulation.EndEpoch = simulation.StartEpoch + random.Next(0, availableMinutes);
            simulation.RunTimeStart = DateTime.Now;
            simulation.ChartId = chart.Id;

            computerContext.Init(chart, simulation.StartEpoch);
            RunSimulation(chart, simulation, computerContext, workerDbContext);
        }
    }

    private void RunSimulation(Chart chart, Simulation simulation, ComputerContext computerContext, MoneyTreeDbContext dbContext)
    {
        Console.WriteLine($"Running simulation for:\n{chart}\nand simulation:\n{simulation}");

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

                SimulationLog(simulation, "Deposit", "$100.00", computerContext.EvaluationEpoch, dbContext);

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
                            SimulationLog(  simulation,
                                            $"Buy {actionToTake.relevantSymbol}",
                                            $"Cash on Hand: {cashOnHand}, qty: {qtyToBuy} at {actionToTake.symbolUsdValue.Value.ToString("c")}",
                                            computerContext.EvaluationEpoch,
                                            dbContext);
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
                            SimulationLog(  simulation,
                                            $"Buy {actionToTake.relevantSymbol}",
                                            $"Cash on Hand: {cashOnHand}, qty: {qtyToBuy} at {actionToTake.symbolUsdValue.Value.ToString("c")}" 
                                                + $", mediatedBuyPrice: {mediatedBuyPrice.ToString("c")}, totalQty: {totalQty}",
                                            computerContext.EvaluationEpoch,
                                            dbContext);
                        }

                        cashOnHand = 0m;
                        break;
                    case ActionToTake.Sell:
                        var asset = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                        cashOnHand += actionToTake.symbolUsdValue.Value * asset.quantityOwned;
                        assets.Remove(asset);
                        SimulationLog(  simulation,
                                        $"Sell {asset.symbol}",
                                        $"Cash on Hand Now: {cashOnHand}, qty: {asset.quantityOwned} at {actionToTake.symbolUsdValue.Value.ToString("c")}",
                                        computerContext.EvaluationEpoch,
                                        dbContext);
                        break;
                    case ActionToTake.Hold:
                        break;
                }
            }

            computerContext.NextTick();

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
            var marketValue = _computer.MarketValue(
                asset.symbol,
                computerContext.EvaluationEpoch,
                dbContext);

            cashOnHand += asset.quantityOwned * marketValue;

            SimulationLog(  simulation,
                            $"Finished, Evaluating {asset.symbol}",
                            $"Cash on Hand Now: {cashOnHand}, qty: {asset.quantityOwned} at {marketValue.ToString("c")}",
                            computerContext.EvaluationEpoch,
                            dbContext);
        }

        if (cashDeposited == 0)
        {
            Console.WriteLine("No cash was deposited in simulation - how is this possible?");
            Console.WriteLine("This is the simulation that failed us:");
            Console.WriteLine(simulation);

            throw new Exception();
        }

        var gain = (cashOnHand - cashDeposited) / cashDeposited;
        simulation.ResultGainPercentage = gain;

        dbContext.Insert(simulation);

        Console.WriteLine("\n---------------\nSimulation:\n---------------");
        Console.WriteLine(simulation.ToString());
    }

    private static void SimulationLog(
        Simulation simulation,
        string action,
        string message,
        int evalTimeEpoch,
        MoneyTreeDbContext dbContext)
    {
        var evalTime = Constants.Epoch.AddMinutes(evalTimeEpoch);
        var log = new SimulationLog
        {
            SimulationId = simulation.Id,
            Time = evalTime,
            Action = action,
            Message = message
        };

        dbContext.Insert(log);
    }
}