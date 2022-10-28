using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using CStafford.MoneyTree.Configuration;
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
        public DbSet<Chart> Charts { get; set; }
        public DbSet<Simulation> Simulations { get; set; }
        public DbSet<SimulationLog> SimulationLogs { get; set; }

        private static ConcurrentDictionary<int, Dictionary<int, (decimal closePrice, decimal volumeUsd)>>
            _ticksAtEpochCache = new ConcurrentDictionary<int, Dictionary<int, (decimal closePrice, decimal volumeUsd)>>();

        private readonly DbConnection _connection;

        public MoneyTreeDbContext(DbContextOptions<MoneyTreeDbContext> options) : base(options)
        {
            _connection = Database.GetDbConnection();
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tick>()
                .HasKey(x => new { x.TickEpoch, x.SymbolId });
            
            modelBuilder.Entity<Tick>()
                .HasIndex(x => new { x.SymbolId, x.TickEpoch });
            
            modelBuilder.Entity<Tick>()
                .HasIndex(x => new { x.TickEpoch });
        }

        public async Task Insert(Symbol symbol)
        {
            using var transaction = _connection.BeginTransaction();
            await _connection.InsertAsync(symbol, transaction);
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

        public void Insert(Simulation simulation)
        {
            _connection.Insert(simulation);
        }

        public void Insert(SimulationLog simulationLog)
        {
            _connection.Insert(simulationLog);
        }

        public IEnumerable<(int symbolId, int lastEpoch)> GetLastEpochForEachSymbol()
        {
            return _connection.Query<(int symbolId, int lastEpoch)>(
                "SELECT SymbolId, MAX(TickEpoch) AS LastEpoch FROM Ticks GROUP BY SymbolId");
        }

        public IEnumerable<(int SymbolId, decimal VolumeUsd)> GetSymbolIdToVolume(
            int startEpoch, 
            int endEpoch)
        {
            const string sql = 
                "select SymbolId, sum(VolumeUsd) " +
                "from Ticks where TickEpoch >= @startEpoch and TickEpoch <= @endEpoch group by SymbolId";

            return _connection.Query<(int, decimal)>(sql, new { startEpoch, endEpoch });
        }

        public IEnumerable<int> FindSymbolsInExistence(int existenceEpoch)
        {
            const string sql = "select SymbolId, min(TickEpoch) from Ticks group by SymbolId";

            var minDates = _connection.Query<(int symbolId, int minTickEpoch)>(sql);
            return minDates.Where(x => x.minTickEpoch <= existenceEpoch).Select(x => x.symbolId).ToList();
        }

        public decimal MarketValue(int symbolId, int dateEpoch)
        {
            const string sql = 
                "select ClosePrice from Ticks where SymbolId = @symbolId and TickEpoch <= @dateEpoch order by TickEpoch desc limit 1";

            return _connection.QueryFirst<decimal>(sql, new { symbolId, dateEpoch });
        }

        public Dictionary<int, (decimal closePrice, decimal volumeUsd)> GetTicksAt(int dateEpoch)
        {
            const string sql = "select SymbolId, ClosePrice, VolumeUsd from Ticks where TickEpoch = @epoch";
                
            return _ticksAtEpochCache.GetOrAdd(dateEpoch, epoch =>
            {
                var ticks = _connection.Query<(int symbolId, decimal close, decimal volumeUsd)>(sql, new { epoch });
                return ticks.ToDictionary(x => x.symbolId, x => (x.close, x.volumeUsd));
            });
        }

        public int GetBestSimulatedChart()
        {
            var last60 = (int)((DateTime.Now.Subtract(TimeSpan.FromDays(60)) - Constants.Epoch).TotalMinutes);
            var sql = @"select 
                        ChartId,
                        avg(ResultGainPercentage / (EndEpoch - StartEpoch)) slope,
                        count(*) as simruns
                        from `Simulations`
                        where StartEpoch > " + last60 + @"
                        group by ChartId
                        order by slope desc";
            
            return _connection.QueryFirst<int>(sql);
        }
    }
}