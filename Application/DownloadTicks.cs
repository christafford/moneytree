using CStafford.MoneyTree.Configuration;
using CStafford.MoneyTree.Infrastructure;
using CStafford.MoneyTree.Models;
using CStafford.MoneyTree.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CStafford.MoneyTree.Application
{
    public class DownloadTicks
    {
        private BinanceApiService _api;
        private ILogger<DownloadTicks> _logger;
        private MoneyTreeDbContext _context;

        public DownloadTicks(
            BinanceApiService api,
            MoneyTreeDbContext context,
            ILogger<DownloadTicks> logger)
        {
            _api = api;
            _context = context;
            _logger = logger;
        }

        public async Task Run()
        {
            var symbols = await GetSymbols();
            Console.WriteLine($"Retrieved {symbols.Count()} symbols");

            var lastRun = _context
                .GetLastEpochForEachSymbol()
                .ToDictionary(x => x.symbolId, x => x.lastEpoch);
            
            symbols.Where(x => !lastRun.ContainsKey(x.Id))
                .ToList()
                .ForEach(x => lastRun.Add(x.Id, 0));

            var symbolsDone = new HashSet<string>();

            // junk
            symbolsDone.Add("TUSD");
            symbolsDone.Add("USDTUSD");
            symbolsDone.Add("USDCUSD");
            symbolsDone.Add("USDCBUSD");

            while (symbols.Any(x => !symbolsDone.Contains(x.Name)))
            {
                foreach (var symbol in symbols.Where(x => !symbolsDone.Contains(x.Name)))
                {
                    var lastRunEnded = lastRun[symbol.Id];

                    // Console.WriteLine($"Symbol {symbol.Name} last response date {lastRunEnded.ToString("g")}");

                    var ticks = await _api.GetTicks(symbol, Constants.Epoch.AddMinutes(lastRunEnded + 1));

                    if (!ticks.Any())
                    {
                        symbolsDone.Add(symbol.Name);
                        // Console.WriteLine($"All caught up with symbol {symbol.Name}");
                        continue;
                    }

                    var minResponseEpoch = ticks.Min(x => x.TickEpoch);
                    var maxResponseEpoch = ticks.Max(x => x.TickEpoch);

                    foreach (var tick in ticks)
                    {
                        tick.SymbolId = symbol.Id;
                        await _context.Insert(tick);
                    }

                    lastRun[symbol.Id] = maxResponseEpoch;

                    // Console.WriteLine($"Symbol {symbol.Name}: saved {ticks.Count()} ticks from " +
                    //     $"{(Constants.Epoch.AddMinutes(minResponseEpoch)).ToString("g")} to " +
                    //     $"{(Constants.Epoch.AddMinutes(maxResponseEpoch)).ToString("g")}");
                }
            }

            Console.WriteLine("Finished - all ticks up to present recorded for all symbols");
        }

        private async Task<IEnumerable<Symbol>> GetSymbols()
        {
            var symbols = (await _api.GetSymbols()).Where(x => x.Name.EndsWith("USD"));
            var inDb = _context.Symbols.ToDictionary(x => x.Name);
            symbols
                .Where(x => !inDb.ContainsKey(x.Name))
                .ToList()
                .ForEach(x => _context.Insert(x).GetAwaiter().GetResult());
            return await _context.Symbols.ToListAsync();
        }
    }
}