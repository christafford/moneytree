using System.Data;
using System.Data.Common;
using CStafford.MoneyTree.Models;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.MoneyTree.Infrastructure
{
    public class MoneyTreeDbContext : DbContext
    {
        public DbSet<Tick> Ticks { get; set; }
        public DbSet<Symbol> Symbols { get; set; }
        public DbSet<PullDown> PullDowns { get; set; }
        public DbSet<Chart> Charts { get; set; }
        public DbSet<Simulation> Simulations { get; set; }

        private readonly DbConnection _connection;

        public MoneyTreeDbContext(DbContextOptions<MoneyTreeDbContext> options) : base(options)
        {
            _connection = Database.GetDbConnection();
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
        }

        public async Task Insert(Symbol symbol)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.InsertAsync(symbol, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(PullDown pulldown)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.InsertAsync(pulldown, transaction);
            await transaction.CommitAsync();
        }

        public async Task Update(PullDown pulldown)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.UpdateAsync(pulldown, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Tick tick)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.InsertAsync(tick, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Chart chart)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.InsertAsync(chart, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Simulation simulation)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.InsertAsync(simulation, transaction);
            await transaction.CommitAsync();
        }

        public async Task<IEnumerable<(int SymbolId, decimal VolumeUsd)>> GetSymbolIdToVolume(
            int startEpoch, 
            int endEpoch)
        {
            const string sql = 
                "select SymbolId, sum(VolumeUsd) " +
                "from Ticks where TickEpoch >= @startEpoch and TickEpoch <= @endEpoch group by SymbolId";

            return await _connection.QueryAsync<(int, decimal)>(sql, new { startEpoch, endEpoch });
        }

        public async Task<List<int>> FindSymbolsInExistence(int existenceEpoch)
        {
            const string sql = "select SymbolId, min(TickEpoch) from Ticks group by SymbolId";

            var minDates = await _connection.QueryAsync<(int symbolId, int minTickEpoch)>(sql);
            return minDates.Where(x => x.minTickEpoch <= existenceEpoch).Select(x => x.symbolId).ToList();
        }

        public async Task<decimal> MarketValue(int symbolId, int dateEpoch)
        {
            const string sql = 
                "select ClosePrice from Ticks where SymbolId = @symbolId and TickEpoch <= @dateEpoch order by OpenTime desc limit 1";

            return await _connection.QueryFirstAsync<decimal>(sql, new { symbolId, dateEpoch });
        }

        public async Task<List<(int symbolId, decimal closePrice, decimal volumeUsd)>> GetTicksAt(int dateEpoch)
        {
            return (await _connection
                .QueryAsync<(int symbolId, decimal closePrice, decimal volumeUsd)>(
                    "select symbolId, closePrice, volumeUsd from Ticks where TickEpoch = @dateEpoch",
                    new { dateEpoch }))
                .ToList();
        }
    }
}