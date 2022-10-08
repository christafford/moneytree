using CStafford.Moneytree.Infrastructure;
using CStafford.Moneytree.Models;
using CStafford.Moneytree.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CStafford.Moneytree.Application
{
    public class DownloadTicks
    {
        private BinanceApiService _api;
        private ILogger<DownloadTicks> _logger;
        private MoneyTreeDbContext _context;
        private static readonly DateTime InitialStart = new DateTime(2019, 9, 6);

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
            _logger.LogInformation("Retrieved {count} symbols", symbols.Count());

            // fix up prior run
            var unfinishedPullDowns = await _context.PullDowns.Where(x => !x.Finished).ToListAsync();
            foreach (var unfinished in unfinishedPullDowns)
            {
                unfinished.TickResponseEnd = (await _context.Ticks
                    .Where(x => x.PullDownId == unfinished.Id)
                    .MaxAsync(x => x.OpenTime));

                unfinished.Finished = true;
                await _context.Update(unfinished);
            }

            var lastRun = new Dictionary<int, DateTime>();
            foreach (var symbolId in symbols.Select(x => x.Id))
            {
                var maxDate = await _context.PullDowns
                    .Where(x => x.SymbolId == symbolId)
                    .MaxAsync(x => (DateTime?) x.TickResponseEnd);
                    
                lastRun[symbolId] = maxDate ?? InitialStart;
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

                    _logger.LogInformation("Symbol {symbol} last response date {lastRun}", symbol.Name, lastRunEnded.ToString("g"));

                    var ticks = await _api.GetTicks(symbol, lastRunEnded.AddMinutes(1));

                    if (!ticks.Any())
                    {
                        symbolsDone.Add(symbol.Name);
                        _logger.LogInformation("All caught up with symbol {symbol}", symbol.Name);
                        continue;
                    }

                    var minResponseTime = ticks.Min(x => x.OpenTime);
                    var maxResponseTime = ticks.Max(x => x.OpenTime);

                    var pulldown = new PullDown
                    {
                        RunTime = DateTime.UtcNow,
                        SymbolId = symbol.Id,
                        TickRequestTime = lastRunEnded,
                        TickResponseStart = minResponseTime,
                        TickResponseEnd = maxResponseTime
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
                    lastRun[symbol.Id] = maxResponseTime;

                    _logger.LogInformation("Symbol {symbol}: saved {number} ticks from {start} to {end}",
                                        symbol.Name,
                                        ticks.Count(),
                                        minResponseTime.ToString("g"),
                                        maxResponseTime.ToString("g"));
                }
            }

            _logger.LogInformation("Finished - all ticks up to present recorded for all symbols");
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