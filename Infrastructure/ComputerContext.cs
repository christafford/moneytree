using CStafford.Moneytree.Models;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Infrastructure;

public class ComputerContext
{
    private MoneyTreeDbContext _dbContext;
    private Chart _chart;
    private IDictionary<int, decimal> _symbolToVolumeUsd;
    private DateTime _firstTickTime;
    private DateTime _lastTickTime;
    private List<int> _validSymbolIds;
    private IDictionary<int, Tick> _firstTicks;
    private IDictionary<int, Tick> _lastTicks;
    private static Object _lock = new Object();

    public DateTime EvaluationTime => _lastTickTime;

    public async Task Init(MoneyTreeDbContext dbContext, Chart chart, DateTime evaluationTime)
    {
        _dbContext = dbContext;
        _chart = chart;
        _firstTickTime = evaluationTime.Subtract(TimeSpan.FromMinutes(chart.MinutesForMarketAnalysis));
        _lastTickTime = evaluationTime;
        
        var validationDate = evaluationTime.Subtract(TimeSpan.FromDays(chart.DaysSymbolsMustExist));

        _validSymbolIds = await _dbContext.FindSymbolsInExistence(validationDate);

        var volumesTraded = await _dbContext.GetSymbolIdToVolume(_firstTickTime, evaluationTime);
        
        _symbolToVolumeUsd = volumesTraded.ToDictionary(x => x.SymbolId, x => x.VolumeUsd);
    
        foreach (var symbolId in _validSymbolIds)
        {
            if (!_symbolToVolumeUsd.ContainsKey(symbolId))
            {
                _symbolToVolumeUsd.Add(symbolId, 0);
            }
        }

        _firstTicks = await _dbContext.Ticks
            .Where(x => x.OpenTime == _firstTickTime)
            .ToDictionaryAsync(x => x.SymbolId);
 
        if (!_firstTicks.Any())
        {
            throw new Exception($"No ticks found at: {_firstTickTime.ToString("g")}");
        }

        _lastTicks = await _dbContext.Ticks
            .Where(x => x.OpenTime == _lastTickTime)
            .ToDictionaryAsync(x => x.SymbolId);
 
        if (!_lastTicks.Any())
        {
            throw new Exception($"No ticks found at: {_lastTickTime.ToString("g")}");
        }
    }

    public List<(int symbolId, decimal volumeUsd, decimal percentageGain, decimal closePrice)> MarketAnalysis()
    {
        var toReturn = new List<(int symbolId, decimal volumeUsd, decimal percentageGain, decimal closePrice)>();

        foreach (var symbolId in _validSymbolIds)
        {
            var firstClosePrice = (_firstTicks.ContainsKey(symbolId) ? _firstTicks[symbolId].ClosePrice : 0) ?? 0;
            var lastClosePrice = (_lastTicks.ContainsKey(symbolId) ? _lastTicks[symbolId].ClosePrice : 0) ?? 0;
            var percentageGain = firstClosePrice == 0 ? 0 : (lastClosePrice - firstClosePrice) / firstClosePrice;
            
            var volumeUsd = _symbolToVolumeUsd[symbolId];

            toReturn.Add((symbolId, volumeUsd, percentageGain, lastClosePrice));
        }

        return toReturn;
    }

    public async Task<DateTime> NextTick()
    {
        foreach (var symbolId in _firstTicks.Keys)
        {
            var tick = _firstTicks[symbolId];
            _symbolToVolumeUsd[symbolId] -= tick.VolumeUsd;
        }
        
        _firstTickTime = _firstTickTime.AddMinutes(1);
        _lastTickTime = _lastTickTime.AddMinutes(1);

        _firstTicks = await _dbContext.Ticks
            .Where(x => x.OpenTime == _firstTickTime)
            .ToDictionaryAsync(x => x.SymbolId);
 
        if (!_firstTicks.Any())
        {
            throw new Exception($"No ticks found at: {_firstTickTime.ToString("g")}");
        }

        _lastTicks = await _dbContext.Ticks
            .Where(x => x.OpenTime == _lastTickTime)
            .ToDictionaryAsync(x => x.SymbolId);
 
        if (!_lastTicks.Any())
        {
            throw new Exception($"No ticks found at: {_lastTickTime.ToString("g")}");
        }

        foreach (var symbolId in _lastTicks.Keys)
        {
            var tick = _lastTicks[symbolId];
            _symbolToVolumeUsd[symbolId] += tick.VolumeUsd;
        }

        return _lastTickTime;
    }
}
