using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Models
{
    [Index(nameof(SymbolName), nameof(TickResponseEnd))]
    public class PullDown
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
                
        [ForeignKey(nameof(Symbol))]
        public string SymbolName { get; set; }
        [Computed]
        public Symbol Symbol { get; set; }

        public DateTime TickRequestTime { get; set; }
        public DateTime TickResponseStart { get; set; }
        public DateTime TickResponseEnd { get; set; }
        public DateTime RunTime { get; set; }
    }
}