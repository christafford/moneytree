using CStafford.MoneyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace CStafford.MoneyTree.Infrastructure;

public class ComputerContext
{
    private MoneyTreeDbContext _dbContext;
    private Chart _chart;
    private IDictionary<int, decimal> _symbolToVolumeUsd;
    private int _firstTickEpoch;
    private int _lastTickEpoch;
    private List<int> _validSymbolIds;
    private IDictionary<int, (int symbolId, decimal closePrice, decimal volumeUsd)> _firstTicks;
    private IDictionary<int, (int symbolId, decimal closePrice, decimal volumeUsd)> _lastTicks;

    public int EvaluationEpoch => _lastTickEpoch;

    public ComputerContext(MoneyTreeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Init(MoneyTreeDbContext dbContext, Chart chart, int evaluationEpoch)
    {
        _chart = chart;
        _firstTickEpoch = evaluationEpoch - chart.MinutesForMarketAnalysis;
        _lastTickEpoch = evaluationEpoch;
        
        var validationEpoch = evaluationEpoch - (chart.DaysSymbolsMustExist * 24 * 60);

        _validSymbolIds = await dbContext.FindSymbolsInExistence(validationEpoch);

        var volumesTraded = await dbContext.GetSymbolIdToVolume(_firstTickEpoch, _lastTickEpoch);
        
        _symbolToVolumeUsd = volumesTraded.ToDictionary(x => x.SymbolId, x => x.VolumeUsd);
    
        foreach (var symbolId in await dbContext.Symbols.Select(x => x.Id).ToListAsync())
        {
            if (!_symbolToVolumeUsd.ContainsKey(symbolId))
            {
                _symbolToVolumeUsd.Add(symbolId, 0);
            }
        }

        _firstTicks = (await dbContext.GetTicksAt(_firstTickEpoch)).ToDictionary(x => x.symbolId);
        _lastTicks = (await dbContext.GetTicksAt(_lastTickEpoch)).ToDictionary(x => x.symbolId);
    }

    public List<(int symbolId, decimal volumeUsd, decimal percentageGain, decimal closePrice)> MarketAnalysis()
    {
        var toReturn = new List<(int symbolId, decimal volumeUsd, decimal percentageGain, decimal closePrice)>();

        foreach (var symbolId in _validSymbolIds)
        {
            var firstClosePrice = _firstTicks.ContainsKey(symbolId) ? _firstTicks[symbolId].closePrice : 0;
            var lastClosePrice = _lastTicks.ContainsKey(symbolId) ? _lastTicks[symbolId].closePrice : 0;
            var percentageGain = firstClosePrice == 0 ? 0 : (lastClosePrice - firstClosePrice) / firstClosePrice;
            
            var volumeUsd = _symbolToVolumeUsd[symbolId];

            if (lastClosePrice > 0)
            {
                toReturn.Add((symbolId, volumeUsd, percentageGain, lastClosePrice));
            }
        }

        return toReturn;
    }

    public async Task<int> NextTick()
    {
        foreach (var symbolId in _firstTicks.Keys)
        {
            var tick = _firstTicks[symbolId];
            _symbolToVolumeUsd[symbolId] -= tick.volumeUsd;
        }
        
        _firstTickEpoch++;
        _lastTickEpoch++;

        var dbStartTimer = DateTime.Now;

        _firstTicks = (await _dbContext.GetTicksAt(_firstTickEpoch)).ToDictionary(x => x.symbolId);
        _lastTicks = (await _dbContext.GetTicksAt(_lastTickEpoch)).ToDictionary(x => x.symbolId);

        var dbStopTimer = DateTime.Now;

        foreach (var symbolId in _lastTicks.Keys)
        {
            var tick = _lastTicks[symbolId];
            _symbolToVolumeUsd[symbolId] += tick.volumeUsd;
        }

        return _lastTickEpoch;
    }
}
