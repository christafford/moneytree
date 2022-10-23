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
            Console.WriteLine("Retrieved {count} symbols", symbols.Count());

            // fix up prior run
            var unfinishedPullDowns = await _context.PullDowns.Where(x => !x.Finished).ToListAsync();
            foreach (var unfinished in unfinishedPullDowns)
            {
                unfinished.TickEndEpoch = (await _context.Ticks
                    .Where(x => x.PullDownId == unfinished.Id)
                    .MaxAsync(x => x.TickEpoch));

                unfinished.Finished = true;
                await _context.Update(unfinished);
            }

            var lastRun = new Dictionary<int, int>();
            foreach (var symbolId in symbols.Select(x => x.Id))
            {
                var maxEpoch = await _context.PullDowns
                    .Where(x => x.SymbolId == symbolId)
                    .MaxAsync(x => (int?) x.TickEndEpoch);
                    
                lastRun[symbolId] = maxEpoch ?? 0;
            }

            var symbolsDone = new HashSet<string>();

            // no idea why but this isn't valid
            symbolsDone.Add("TUSD");

            // and this isn't useful
            symbolsDone.Add("USDTUSD");

            while (symbols.Any(x => !symbolsDone.Contains(x.Name)))
            {
                foreach (var symbol in symbols.Where(x => !symbolsDone.Contains(x.Name)))
                {
                    var lastRunEnded = lastRun[symbol.Id];

                    Console.WriteLine("Symbol {symbol} last response date {lastRun}", symbol.Name, lastRunEnded.ToString("g"));

                    var ticks = await _api.GetTicks(symbol, Constants.Epoch.AddMinutes(lastRunEnded + 1));

                    if (!ticks.Any())
                    {
                        symbolsDone.Add(symbol.Name);
                        Console.WriteLine("All caught up with symbol {symbol}", symbol.Name);
                        continue;
                    }

                    var minResponseEpoch = ticks.Min(x => x.TickEpoch);
                    var maxResponseEpoch = ticks.Max(x => x.TickEpoch);

                    var pulldown = new PullDown
                    {
                        RunTime = DateTime.UtcNow,
                        SymbolId = symbol.Id,
                        TickStartEpoch = minResponseEpoch,
                        TickEndEpoch = maxResponseEpoch
                    };

                    await _context.Insert(pulldown);

                    foreach (var tick in ticks)
                    {
                        tick.PullDownId = pulldown.Id;
                        tick.SymbolId = symbol.Id;
                        await _context.Insert(tick);
                    }

                    pulldown.Finished = true;
                    await _context.Update(pulldown);
                    lastRun[symbol.Id] = maxResponseEpoch;

                    Console.WriteLine($"Symbol {symbol.Name}: saved {ticks.Count()} ticks from " +
                        $"{(Constants.Epoch.AddMinutes(minResponseEpoch)).ToString("g")} to " +
                        $"{(Constants.Epoch.AddMinutes(maxResponseEpoch)).ToString("g")}");
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