using System.ComponentModel.DataAnnotations.Schema;
using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.MoneyTree.Models
{
    public class PullDown
    {
        [Dapper.Contrib.Extensions.Key]
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public int SymbolId { get; set; }
        public int TickStartEpoch { get; set; }
        public int? TickEndEpoch { get; set; }
        public DateTime RunTime { get; set; }
        public bool Finished { get; set; }
    }
}