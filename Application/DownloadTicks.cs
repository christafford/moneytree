using CStafford.Moneytree.Infrastructure;
using CStafford.Moneytree.Models;
using CStafford.Moneytree.Services;
using Microsoft.Extensions.Logging;

namespace CStafford.Moneytree.Application
{
    public class DownloadTicks
    {
        private BinanceApiService _api;
        private ILogger<DownloadTicks> _logger;
        private MoneyTreeDbContext _context;
        private static readonly DateTime InitialStart = new DateTime(2018, 1, 1);

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

            var symbolsDone = new HashSet<string>();

            // no idea why but this isn't valid
            symbolsDone.Add("TUSD");

            while (symbols.Any(x => !symbolsDone.Contains(x.Name)))
            {
                foreach (var symbol in symbols.Where(x => !symbolsDone.Contains(x.Name)))
                {
                    var lastRunQuery = _context.PullDowns
                        .Where(x => x.SymbolName == symbol.Name)
                        .OrderByDescending(x => x.TickResponseEnd)
                        .Select(x => (DateTime?) x.TickResponseEnd)
                        .FirstOrDefault();
                    
                    var lastRunEnded = lastRunQuery ?? InitialStart;

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
                        SymbolName = symbol.Name,
                        TickRequestTime = lastRunEnded,
                        TickResponseStart = minResponseTime,
                        TickResponseEnd = maxResponseTime
                    };

                    await _context.Insert(pulldown);

                    foreach (var tick in ticks)
                    {
                        tick.PullDownId = pulldown.Id;
                        tick.SymbolName = symbol.Name;
                        await _context.Insert(tick);
                    }

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
                .ForEach(x => _context.Symbols.Add(x));
            await _context.SaveChangesAsync();
            return symbols;
        }
    }
}