using System.Data;
using System.Data.Common;
using CStafford.Moneytree.Models;
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