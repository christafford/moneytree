using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.Moneytree.Models
{

    [Index(nameof(OpenTime))]
    [Index(nameof(SymbolName), nameof(OpenTime))]
    public class Tick
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public string SymbolName { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal? OpenPrice { get; set; }
        public decimal? HighPrice { get; set; }
        public decimal? LowPrice { get; set; }
        public decimal? ClosePrice { get; set; }
        public decimal? Volume { get; set; }
        
        [ForeignKey(nameof(PullDown))]
        public int PullDownId { get; set; }
        [Computed]
        public PullDown PullDown { get; set; }
    }
}