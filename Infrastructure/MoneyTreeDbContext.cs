using System.Data;
using System.Data.Common;
using CStafford.Moneytree.Models;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Infrastructure
{
    public class MoneyTreeDbContext : DbContext
    {
        public MoneyTreeDbContext(DbContextOptions<MoneyTreeDbContext> options) : base(options) { }

        public DbSet<Tick> Ticks { get; set; }
        public DbSet<Symbol> Symbols { get; set; }
        public DbSet<PullDown> PullDowns { get; set; }
        public DbSet<Chart> Charts { get; set; }
        public DbSet<Simulation> Simulations { get; set; }

        public async Task Insert(Symbol symbol)
        {
            var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(symbol, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(PullDown pulldown)
        {
            var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(pulldown, transaction);
            await transaction.CommitAsync();
        }

        public async Task Update(PullDown pulldown)
        {
            var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            await connection.UpdateAsync(pulldown, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Tick tick)
        {
            var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(tick, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Chart chart)
        {
            var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(chart, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Simulation simulation)
        {
            var connection = GetConnection();
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(simulation, transaction);
            await transaction.CommitAsync();
        }

        public async Task<IEnumerable<(int SymbolId, decimal VolumeUsd)>> GetSymbolIdToVolume(DateTime start, DateTime end)
        {
            const string sql = 
                "select SymbolId, sum(VolumeUsd) " +
                "from Ticks where OpenTime >= @start and OpenTime <= @end group by SymbolId";

            var connection = GetConnection();
            return await connection.QueryAsync<(int, decimal)>(sql, new { start, end });
        }

        public async Task<List<int>> FindSymbolsInExistence(DateTime existenceDate)
        {
            const string sql = "select SymbolId, min(OpenTime) from Ticks group by SymbolId";
            var connection = GetConnection();
            var minDates = await connection.QueryAsync<(int SymbolId, DateTime minDate)>(sql);
            return minDates.Where(x => x.minDate <= existenceDate).Select(x => x.SymbolId).ToList();
        }

        public async Task<decimal> MarketValue(int symbolId, DateTime atDate)
        {
            const string sql = 
                "select ClosePrice from Ticks where SymbolId = @symbolId and OpenTime <= @atDate order by OpenTime desc limit 1";

            var connection = GetConnection();
            return await connection.QueryFirstAsync<decimal>(sql, new { symbolId, atDate });
        }

        private DbConnection GetConnection()
        {
            var connection = Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            
            return connection;
        }
    }
}