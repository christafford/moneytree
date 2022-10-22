using CStafford.MoneyTree.Configuration;
using CStafford.MoneyTree.Models;
using Dapper;
using Dapper.Contrib.Extensions;
using MySqlConnector;

namespace CStafford.MoneyTree;

public class PortFromOld
{
    public static async Task Port()
    {
        var oldConnectionString = $"Server=127.0.0.1;Port=3306;Database=MoneyTree;User=root;Password=qwe123;SSL Mode=None;AllowPublicKeyRetrieval=True;default command timeout=0;";
        var newConnectionString = $"Server=127.0.0.1;Port=3306;Database=MoneyTree2;User=root;Password=qwe123;SSL Mode=None;AllowPublicKeyRetrieval=True;default command timeout=0;";

        var oldConnection = new MySqlConnection(oldConnectionString);
        var newConnection = new MySqlConnection(newConnectionString);

        Console.WriteLine("Doing symbols");
        var symbolMap = new Dictionary<int, int>();
        var oldSymbols = oldConnection.Query<Symbol>("SELECT * FROM Symbols");
        foreach (var symbol in oldSymbols)
        {
            var newSymbol = new Symbol
            {
                Name = symbol.Name,
                MinTradeQuantity = symbol.MinTradeQuantity,
                QuantityStep = symbol.QuantityStep,
                PriceStep = symbol.PriceStep,
                QuantityDecimals = symbol.QuantityDecimals,
                PriceDecimals = symbol.PriceDecimals
            };

            newConnection.Insert(newSymbol);
            symbolMap[symbol.Id] = newSymbol.Id;            
        }

        var pulldownMap = new Dictionary<int, int>();
        
        Console.WriteLine("Doing ticks");
        
        var currentDay = oldConnection.QuerySingle<DateTime>("select min(openTime) from Ticks").Date;
        
        var selectOldMs = 0d;
        var pulldownMs = 0d;
        var insertNewTicksMs = 0d;
        var grandTotal = 108250962;
        var count = 0;

        while (true)
        {
            var sql = $"select * from `Ticks` where opentime >= '{ToMySql(currentDay)}' and " +
                $"opentime < '{ToMySql(currentDay.AddDays(1))}' order by opentime, symbolid";
            
            var time = DateTime.Now;

            var ticks = oldConnection.Query<OldTick>(sql).ToList();
            
            if (!ticks.Any())
            {
                break;
            }

            selectOldMs += (DateTime.Now - time).TotalMilliseconds;

            foreach (var oldTick in ticks)
            {
                count++;

                var symbolId = symbolMap[oldTick.SymbolId];
                var epoch = ToEpoch(oldTick.OpenTime);

                if (!pulldownMap.ContainsKey(oldTick.PullDownId))
                {
                    Console.WriteLine("grabbing pulldown");
                    time = DateTime.Now;
                    
                    var sqlPulldown = $"select * from PullDowns where id = {oldTick.PullDownId}";

                    var pulldown = oldConnection.QuerySingle<OldPulldown>(sqlPulldown);
                    var newPulldown = new PullDown
                    {
                        SymbolId = symbolMap[pulldown.SymbolId],
                        TickStartEpoch = ToEpoch(pulldown.TickRequestTime),
                        TickEndEpoch = ToEpoch(pulldown.TickResponseEnd),
                        RunTime = pulldown.RunTime,
                        Finished = pulldown.Finished
                    };

                    newConnection.Insert(newPulldown);
                    pulldownMap[pulldown.Id] = newPulldown.Id;

                    pulldownMs += (DateTime.Now - time).TotalMilliseconds;
                }

                time = DateTime.Now;
                
                var newTick = new Tick
                {
                    TickEpoch = ToEpoch(oldTick.OpenTime),
                    SymbolId = symbolId,
                    ClosePrice = oldTick.ClosePrice ?? 0,
                    VolumeUsd = (oldTick.Volume ?? 0) * (oldTick.ClosePrice ?? 0),
                    PullDownId = pulldownMap[oldTick.PullDownId]
                };

                newConnection.Insert(newTick);

                insertNewTicksMs += (DateTime.Now - time).TotalMilliseconds;
            }

            Console.WriteLine("--------------------");
            Console.WriteLine($"Finished day {currentDay.ToString("g")}");
            Console.WriteLine($"Total selectOldMs: {selectOldMs}");
            Console.WriteLine($"Total pulldownMs: {pulldownMs}");
            Console.WriteLine($"Total insertNewTicksMs: {insertNewTicksMs}");
            Console.WriteLine($"Did {count} of {grandTotal} ticks - {count / grandTotal * 100d}%");

            selectOldMs = 0d;
            pulldownMs = 0d;
            insertNewTicksMs = 0d;
            currentDay = currentDay.AddDays(1);
        }

        Console.WriteLine("Done");
    }

    private static int ToEpoch(DateTime dateTime)
    {
        return (int) dateTime.Subtract(Constants.Epoch).TotalMinutes;
    }

    private static string ToMySql(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public class OldPulldown
    {
        public int Id { get; set; }
        public int SymbolId { get; set; }
        public DateTime TickRequestTime { get; set; }
        public DateTime TickResponseStart { get; set; }
        public DateTime TickResponseEnd { get; set; }
        public DateTime RunTime { get; set; }
        public bool Finished { get; set; }
    }

    public class OldTick
    {
        public long Id { get; set; }
        public int SymbolId { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? ClosePrice { get; set; }
        public decimal? Volume { get; set; }
        public int PullDownId { get; set; }
        public decimal VolumeUsd { get; set; }
    }
}
