using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore;

namespace CStafford.MoneyTree.Models
{
    //[Index(nameof(TickEpoch))]
    //[Index(nameof(SymbolId))]
    public class Tick
    {
        [ExplicitKey]
        public int TickEpoch { get; set; }
        [ExplicitKey]
        public int SymbolId { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal VolumeUsd { get; set; }
    }
}