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

        public async Task<IEnumerable<(int SymbolId, decimal VolumeUsd)>> GetSymbolIdToVolume(DateTime start, DateTime end)
        {
            const string sql = 
                "select SymbolId, sum(VolumeUsd) " +
                "from Ticks where OpenTime >= @start and OpenTime <= @end group by SymbolId";

            return await _connection.QueryAsync<(int, decimal)>(sql, new { start, end });
        }

        public async Task<List<int>> FindSymbolsInExistence(DateTime existenceDate)
        {
            const string sql = "select SymbolId, min(OpenTime) from Ticks group by SymbolId";

            var minDates = await _connection.QueryAsync<(int SymbolId, DateTime minDate)>(sql);
            return minDates.Where(x => x.minDate <= existenceDate).Select(x => x.SymbolId).ToList();
        }

        public async Task<decimal> MarketValue(int symbolId, DateTime atDate)
        {
            const string sql = 
                "select ClosePrice from Ticks where SymbolId = @symbolId and OpenTime <= @atDate order by OpenTime desc limit 1";

            return await _connection.QueryFirstAsync<decimal>(sql, new { symbolId, atDate });
        }

        public async Task<List<Tick>> GetTicksAt(DateTime dateAt)
        {
            return (await _connection
                .QueryAsync<Tick>(
                    "select * from Ticks where OpenTime = @dateAt",
                    new { dateAt }))
                .ToList();
        }
    }
}