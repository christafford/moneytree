using System.Text.RegularExpressions;
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
        var chartId = 590; // _dbContext.GetBestSimulatedChart();
        var chart = _dbContext.Charts.First(x => x.Id == chartId);
        var bnbusdId = _dbContext.Symbols.First(x => x.Name == "BNBUSD").Id;
        
        Console.WriteLine("Using this chart to guide us");
        Console.WriteLine(chart.ToString());
        
        var assets = new List<(string symbol, decimal usdInvested, decimal quantityOwned)>();
        
        var lastTransaction = ReadLast();
        if (lastTransaction.action == "Bought")
        {
            var primaryWallet = _binance.GetPrimary().GetAwaiter().GetResult();

            if (primaryWallet.coin != lastTransaction.coin)
            {
                throw new Exception($"Primary wallet coin is {primaryWallet.coin}, last action coin is {lastTransaction.coin}");
            }

            assets.Add((lastTransaction.coin + "USD", lastTransaction.paidUsd, lastTransaction.qty));
            Console.WriteLine($"starting with asset {lastTransaction.coin + "USD"} - {lastTransaction.qty}, paid {lastTransaction.paidUsd}");
        }
        
        Log("Starting the machine");

        while (true)
        {
            var startLoop = DateTime.Now;
            Console.WriteLine("Updating ticks");
            _downloader.Run().GetAwaiter().GetResult();
            
            var lastEpoch = _dbContext.Ticks.Max(x => x.TickEpoch);
            var cashOnHand = _binance.GetAsset("USD").Result;

            Console.WriteLine($"Cash on hand: {cashOnHand.ToString("C")}");

            if (cashOnHand > 500)
            {
                var bnb = _binance.GetAsset("BNB").Result;
                var exchangeRate = _dbContext.Ticks
                    .Where(x => x.SymbolId == bnbusdId)
                    .OrderByDescending(x => x.TickEpoch)
                    .First()
                    .ClosePrice;

                if (bnb * exchangeRate < 20)
                {
                    Log("Buying $20 BNB to pay for fees");
                    _binance.DoBuy("BNB", 20).GetAwaiter().GetResult();
                    cashOnHand = _binance.GetAsset("USD").Result;
                    Log($"Cash on hand now: {cashOnHand.ToString("C")}");
                }
            }

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
                        var symbol = _dbContext.Symbols.First(x => x.Name == asset.symbol);
                        var qty = asset.quantityOwned;
                        (decimal usdValue, decimal qtySold) sellResult;
                        
                        while (true)
                        {  
                            if (symbol.QuantityStep.HasValue)
                            {
                                var numDecimals = 0;
                                var stepStr = symbol.QuantityStep.Value.ToString();
                                var decimalPoint = stepStr.IndexOf('.');
                                
                                for (var i = 0; i < stepStr.Length - decimalPoint; i++)
                                {
                                    if (stepStr[decimalPoint + i] == '1')
                                    {
                                        numDecimals = i;
                                        break;
                                    }
                                }

                                qty = Decimal.Round(qty, numDecimals, MidpointRounding.ToZero);
                            }

                            try
                            {
                                sellResult = _binance.DoSell(coin, qty).GetAwaiter().GetResult();
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("Account has insufficient balance for requested action"))
                                {
                                    Log("Insufficient balance, lowering qty by 1%");
                                    qty = qty * 0.99m;
                                }
                                else
                                {
                                    throw;
                                }
                            }
                        }

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

    public void Log(string message)
    {
        Console.WriteLine($"{DateTime.Now.ToString("g")}: {message}");
        File.AppendAllText("/home/chris/code/moneytree/real-trading.log", $"{DateTime.Now.ToString("g")}: {message}\n");
    }

    private (string coin, string action, decimal qty, decimal paidUsd) ReadLast()
    {
        var text = File.ReadAllText("/home/chris/code/moneytree/real-trading.log");
        var regex = new Regex("(Bought|Sold) ([0-9.]+) of ([A-Z]+) for .?([0-9.]+)");

        var match = regex.Match(text);
        
        while (true)
        {
            var nextMatch = match.NextMatch();
            if (nextMatch.Success)
            {
                match = nextMatch;
            }
            else
            {
                break;
            }
        }
        
        var action = match.Result("$1");
        var qty = decimal.Parse(match.Result("$2"));
        var coin = match.Result("$3");
        var paidUsd = decimal.Parse(match.Result("$4"));

        return (coin, action, qty, paidUsd);
    }
}
