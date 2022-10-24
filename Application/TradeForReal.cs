using CStafford.MoneyTree.Application;
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
        Console.Write("Enter chart id: ");
        
        var chartId = int.Parse(Console.ReadLine());
        var chart = _dbContext.Charts.First(x => x.Id == chartId);
        
        Console.WriteLine(chart.ToString());
        Console.WriteLine("Press enter to begin...");
        Console.ReadLine();
        
        var assets = new List<(string symbol, decimal usdPurchasePrice, decimal quantityOwned)>();
        
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
                assets.Select(x => (x.symbol, x.usdPurchasePrice)).ToList(),
                computerContext);

            decimal fees = default;

            foreach (var actionToTake in actionsToTake)
            {
                var coin = actionToTake.relevantSymbol.Substring(0, actionToTake.relevantSymbol.Length - 3);

                switch (actionToTake.action)
                {
                    case ActionToTake.Buy:
                        if (! assets.Any(x => x.symbol == actionToTake.relevantSymbol))
                        {
                            fees = Constants.FeeRate * cashOnHand;

                            var qtyToBuy = (cashOnHand - fees) / actionToTake.symbolUsdValue.Value;

                            var buyResult = _binance.DoBuy(coin, cashOnHand).GetAwaiter().GetResult();
                            
                            Console.WriteLine($"Bought {buyResult.qtyBought.ToString("0.####")} of {coin} for {buyResult.usdValue.ToString("C")}");
                            Console.WriteLine($"Estimated qty: {qtyToBuy.ToString("0.####")}");

                            assets.Add((actionToTake.relevantSymbol, actionToTake.symbolUsdValue.Value, qtyToBuy));
                        }
                        else
                        {
                            var existing = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                            assets.Remove(existing);

                            fees = Constants.FeeRate * cashOnHand;
                            
                            // calculate a viable usdPurchasePrice proportional to what's owned and what's being bought now
                            var qtyToBuy = (cashOnHand - fees) / actionToTake.symbolUsdValue.Value;
                            var totalQty = qtyToBuy + existing.quantityOwned;
                            var mediatedBuyPrice = ((existing.usdPurchasePrice * existing.quantityOwned) + 
                                                        (actionToTake.symbolUsdValue.Value * qtyToBuy))
                                                    / totalQty;

                            var buyResult = _binance.DoBuy(coin, cashOnHand).GetAwaiter().GetResult();
                            
                            Console.WriteLine($"Bought {buyResult.qtyBought.ToString("0.####")} of {coin} for {buyResult.usdValue.ToString("C")}");
                            Console.WriteLine($"Estimated qty: {totalQty.ToString("0.####")}");

                            assets.Add((actionToTake.relevantSymbol, mediatedBuyPrice, totalQty));
                        }
                        break;

                    case ActionToTake.Sell:
                        var asset = assets.First(x => x.symbol == actionToTake.relevantSymbol);
                        var sellValue = actionToTake.symbolUsdValue.Value * asset.quantityOwned;
                        fees = Constants.FeeRate * sellValue;
                        
                        var sellResult = _binance.DoSell(coin, asset.quantityOwned).GetAwaiter().GetResult();
                        
                        Console.WriteLine($"Sold {sellResult.qtySold.ToString("0.####")} of {coin} for {sellResult.usdValue.ToString("C")}");
                        
                        assets.Remove(asset);
                        break;

                    case ActionToTake.Hold:
                        break;
                }
            }

            computerContext.NextTick();

            // make sure we run loop only once per minute
            if (startLoop.AddMinutes(1) > DateTime.Now)
            {
                Thread.Sleep(startLoop.AddMinutes(1).Subtract(DateTime.Now));
            }
        }
    }
}
