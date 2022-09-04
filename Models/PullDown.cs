using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Models
{
    [Index(nameof(SymbolName), nameof(TickResponseEnd))]
    public class PullDown
    {
        [Key]
        public int Id { get; set; }
                
        [ForeignKey("Symbol")]
        public string SymbolName { get; set; }
        public Symbol Symbol { get; set; }

        public DateTime TickRequestTime { get; set; }
        public DateTime TickResponseStart { get; set; }
        public DateTime TickResponseEnd { get; set; }
        public DateTime RunTime { get; set; }
    }
}