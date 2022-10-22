using Microsoft.EntityFrameworkCore;

namespace CStafford.MoneyTree.Models
{
    public class Tick
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public long Id { get; set; }
        public int TickEpoch { get; set; }
        public int SymbolId { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal VolumeUsd { get; set; }
        public int PullDownId { get; set; }
    }
}