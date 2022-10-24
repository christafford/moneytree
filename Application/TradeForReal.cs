using CStafford.MoneyTree.Configuration;
using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Services;
using static CStafford.MoneyTree.Application.Computer;

namespace CStafford.MoneyTree.Application;

public class TradeForReal
{
    private readonly DownloadTicks _downloader;
    private readonly Computer _computer;
    private readonly BinanceApiService _binance;
    private readonly MoneyTreeDbContext _dbContext;

    public TradeForReal(DownloadTicks downloader, Computer computer, BinanceApiService binance, MoneyTreeDbContext dbContext)
    {
        _downloader = downloader;
        _computer = computer;
        _binance = binance;
        _dbContext = dbContext;
    }

    public void Run()
    {
        var chartId = 590; //_dbContext.GetBestSimulatedChart();
        var chart = _dbContext.Charts.First(x => x.Id == chartId);
        
        Console.WriteLine("Using this chart to guide us");
        Console.WriteLine(chart.ToString());
        
        var assets = new List<(string symbol, decimal usdInvested, decimal quantityOwned)>();

        Log("Starting the machine");

        while (true)
        {
            var startLoop = DateTime.Now;
            Console.WriteLine("Updating ticks");
            _downloader.Run().GetAwaiter().GetResult();
            
            var lastEpoch = _dbContext.Ticks.Max(x => x.TickEpoch);
            var cashOnHand = _binance.GetCashOnHand().Result;

            Console.WriteLine($"Cash on hand: {cashOnHand.ToString("C")}");

            var computerContext = new ComputerContext(_dbContext);
            computerContext.Init(chart, lastEpoch);
            
            var actionsToTake = _computer.EvaluateMarket(
                chart,
                cashOnHand > 20m,
                assets.Select(x => (x.symbol, x.usdInvested / x.quantityOwned)).ToList(),
                computerContext);

            decimal fees = default;

            foreach (var actionToTake in actionsToTake)
            {
                var coin = actionToTake.relevantSymbol.Substring(0, actionToTake.relevantSymbol.Length - 3);

                switch (actionToTake.action)
                {
                    case ActionToTake.Buy:
                        Log("--------------->");
                        if (! assets.Any(x => x.symbol == actionToTake.relevantSymbol))
                        {
                            fees = Constants.FeeRate * cashOnHand;

                            var buyResult = _binance.DoBuy(coin, cashOnHand).GetAwaiter().GetResult();
                            Log($"Bought {buyResult.qtyBought.ToString("0.####")} of {coin} for {buyResult.usdValue.ToString("C")}");

                            assets.Add((
                                actionToTake.relevantSymbol,
                                buyResult.usdValue,
                                buyResult.qtyBought));
                        }
                        else
                        {
                            var existing = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                            assets.Remove(existing);

                            fees = Constants.FeeRate * cashOnHand;
                            
                            var buyResult = _binance.DoBuy(coin, cashOnHand).GetAwaiter().GetResult();
                            
                            Log($"Bought {buyResult.qtyBought.ToString("0.####")} of {coin} for {buyResult.usdValue.ToString("C")}");
                            
                            var totalQty = existing.quantityOwned + buyResult.qtyBought;
                        
                            Log($"Already owned {existing.quantityOwned.ToString("0.####")} of {coin} with investment of {existing.usdInvested.ToString("C")}");
                            Log($"Now own {totalQty.ToString("0.####")} with {(existing.usdInvested + buyResult.usdValue).ToString("C")} invested");
                            
                            assets.Add((actionToTake.relevantSymbol, existing.usdInvested + buyResult.usdValue, totalQty));
                        }
                        break;

                    case ActionToTake.Sell:
                        var asset = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                        
                        var sellResult = _binance.DoSell(coin, asset.quantityOwned).GetAwaiter().GetResult();
                        Log("--------------->");
                        Log($"Sold {sellResult.qtySold.ToString("0.####")} of {coin} for {sellResult.usdValue.ToString("C")}");
                        assets.Remove(asset);

                        break;

                    case ActionToTake.Hold:
                        break;
                }
            }

            // make sure we run loop only once per minute
            if (startLoop.AddMinutes(1) > DateTime.Now)
            {
                Thread.Sleep(startLoop.AddMinutes(1).Subtract(DateTime.Now));
            }
        }
    }

    private void Log(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString("g")}: {message}");
        File.AppendAllText("/home/chris/code/moneytree/real-trading.log", $"{DateTime.Now.ToString("g")}: {message}\n");
    }
}
