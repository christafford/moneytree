using CStafford.Moneytree.Models;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Infrastructure;

public class ComputerContext
{
    private MoneyTreeDbContext _dbContext;
    private Chart _chart;
    private IDictionary<int, decimal> _symbolToVolumeUsd;
    private List<Tick> _firstTicksOfSet;
    private List<Tick> _lastTicksOfSet;
    private DateTime _lastTickTime;
    private static List<int> _allSymbolIds;
    private static Object _lock = new Object();

    public async Task Init(MoneyTreeDbContext dbContext, Chart chart, DateTime evaluationTime)
    {
        _dbContext = dbContext;
        _chart = chart;
        _lastTickTime = evaluationTime;
        
        if (_allSymbolIds == null)
        {
            lock (_lock)
            {
                if (_allSymbolIds == null)
                {
                    _allSymbolIds = _dbContext.Symbols.Select(x => x.Id).ToList();
                }
            }
        }

        var start = evaluationTime.Subtract(TimeSpan.FromMinutes(chart.MinutesForMarketAnalysis));
        var volumesTraded = await _dbContext.GetSymbolIdToVolume(start, evaluationTime);
        
        _symbolToVolumeUsd = volumesTraded.ToDictionary(x => x.SymbolId, x => x.VolumeUsd);
    
        foreach (var symbolId in _allSymbolIds)
        {
            if (!_symbolToVolumeUsd.ContainsKey(symbolId))
            {
                _symbolToVolumeUsd.Add(symbolId, 0);
            }
        }

        _firstTicksOfSet = await _dbContext.Ticks.Where(x => x.OpenTime == start).ToListAsync();
        _lastTicksOfSet = await _dbContext.Ticks.Where(x => x.OpenTime == evaluationTime).ToListAsync();
    }

    public SortedList<()
}
