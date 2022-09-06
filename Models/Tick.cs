using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Models
{
    public class Tick
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public long Id { get; set; }
        public int SymbolId { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? ClosePrice { get; set; }
        public decimal? Volume { get; set; }
        public int PullDownId { get; set; }
    }
}