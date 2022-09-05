using System.Data;
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

        public async Task Insert(PullDown pulldown)
        {
            var connection = Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(pulldown, transaction);
            await transaction.CommitAsync();
        }

        public async Task Insert(Tick tick)
        {
            var connection = Database.GetDbConnection();
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }
            using var transaction = connection.BeginTransaction();
            await connection.InsertAsync(tick, transaction);
            await transaction.CommitAsync();
        }
    }
}