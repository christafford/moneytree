using CStafford.Moneytree.Configuration;
using CStafford.Moneytree.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CStafford.Moneytree.Infrastructure
{
    public class MoneyTreeDbContext : DbContext
    {
        public MoneyTreeDbContext(DbContextOptions<MoneyTreeDbContext> options) : base(options) { }

        public DbSet<Tick> Ticks { get; set; }
        public DbSet<Symbol> Symbols { get; set; }
        public DbSet<PullDown> PullDowns { get; set; }
    }
}